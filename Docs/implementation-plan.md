# ICC Profile Viewer 구현 계획

- 문서 상태: Draft v0.1
- 작성일: 2026-07-19
- 대상 제품: MVP는 Windows 10/11 x64용 ICC Profile Viewer. Windows ARM64, macOS, Linux는 MVP 이후 지원

## 1. 결정 사항 요약

이 프로젝트는 다음 기술 구성을 기본으로 한다.

| 구분 | 선택 | 비고 |
|---|---|---|
| 언어/런타임 | C# / .NET 10 LTS | 모든 프로젝트에서 nullable 활성화 |
| UI | Avalonia 12.1.0 | MVP는 Windows x64, 이후 Windows ARM64·macOS·Linux로 확장 가능한 단일 UI 코드베이스 |
| UI 패턴 | MVVM | 화면 상태와 ICC/색도 계산 로직 분리 |
| ICC 엔진 | LittleCMS 2 | ICC v2/v4 처리와 색 변환 담당 |
| .NET 바인딩 | `lcmsNET` 1.2.1 | 구현 시작 시 최신 호환 버전을 다시 확인하고 고정 |
| LittleCMS 소스 | `mm2/Little-CMS` Git submodule | `lcms2.19.1` (`21c582a594fe5279f90c0b93437c398f93bf62b0`)에 고정 |
| 네이티브 준비 | MVP는 Windows 수동 script | `.slnx` build에서는 LittleCMS를 빌드하지 않으며 macOS·Linux 설치 경로는 MVP 이후 마련 |
| 다이어그램 | Avalonia 커스텀 컨트롤 | 벡터 경계선과 캐시된 래스터 배경을 조합 |
| 테스트 | MSTest | 순수 계산 단위 테스트와 네이티브 통합 테스트 분리 |

Avalonia 12는 .NET 10을 권장 대상으로 지원한다. `lcmsNET` 1.2.1은 `net8.0` 및 `netstandard2.0` 자산을 제공하므로 `net10.0`에서 참조할 수 있다.

단, `lcmsNET`은 LittleCMS의 관리형 바인딩이며 실행에 필요한 네이티브 LittleCMS 라이브러리를 모든 대상 플랫폼에 자동으로 제공하는 패키지는 아니다. `.slnx` build에서 LittleCMS를 자동으로 빌드하지 않는다. 개발자는 운영체제 패키지로 LittleCMS를 설치하거나, [mm2/Little-CMS](https://github.com/mm2/Little-CMS) submodule을 수동으로 빌드해 앱 로더가 찾을 수 있는 위치에 둔다. 공개 배포물에 native library를 포함할 경우에도 릴리스 담당자가 수동으로 준비한 artifact를 사용한다.

## 2. 제품 목표

### 2.1 MVP 목표

1. 로컬 ICC/ICM 파일을 열거나 창에 드래그해서 불러온다.
2. ICC v2와 v4 프로필을 모두 인식한다.
3. 다음 프로필 정보를 표시한다.
   - 파일 이름과 크기
   - ICC 버전
   - Profile class
   - Data color space
   - PCS(Profile Connection Space)
   - 생성 시각
   - Primary platform
   - CMM, 제조사, 모델
   - Rendering intent
   - Profile description, copyright
   - White point, black point
   - 태그 목록과 태그 크기
4. CIE 1931 `xy` 및 CIE 1976 `u'v'` 색도도를 탭으로 표시한다.
5. 선택한 RGB 프로필의 색역과 다음 기준 색역을 중첩한다.
   - sRGB
   - Display P3
   - DCI-P3
   - Adobe RGB (1998)
   - BT.2020
6. 각 기준 색역을 개별 체크박스로 켜고 끌 수 있게 한다.
7. 지원하지 못하는 프로필 구조에서는 잘못된 삼각형을 그리지 않고 이유를 설명한다.
8. Windows 10/11 x64용 self-contained 배포물을 생성한다.

### 2.2 MVP 이후 구현 사항

MVP 이후에는 다음 플랫폼 지원을 순차적으로 추가한다. 각 플랫폼을 실제 환경에서 빌드·테스트하기 전까지는 공식 지원으로 표시하지 않는다.

- Windows ARM64
- macOS
- Linux

플랫폼 확장보다도 훨씬 나중에 별도 CLI 도구를 구현한다.

- 프로젝트명: `ICCProfileViewer.Cli`
- 입력 ICC/ICM 프로필의 CIE 1931 `xy` 및 CIE 1976 `u'v'` chromaticity diagram 생성
- 각 diagram을 PNG 또는 SVG 파일로 출력
- `--diagram xy|uv|all`, `--format png|svg`, `--output <path>` 형태의 비대화형 명령 제공
- 데스크톱 앱과 같은 기준 색역 overlay 및 렌더링 설정 재사용
- `ICCProfileViewer.Core`의 색도 계산·레이아웃 결과를 재사용하고 Avalonia UI에는 의존하지 않는 headless 구조

이 CLI는 앱 MVP와 플랫폼 확장 범위 및 일정에 포함하지 않는다.

## 3. 지원 플랫폼

MVP 공식 지원 범위는 다음과 같다.

| 플랫폼 | RID | 지원 수준 |
|---|---|---|
| Windows 10/11 x64 | `win-x64` | 필수 |

MVP 이후 지원 후보는 Windows ARM64, macOS, Linux다. 구체적인 architecture와 RID는 해당 플랫폼 구현을 시작할 때 정하고, 대상 플랫폼에서 빌드·실행·배포 검증을 마친 뒤 공식 지원에 포함한다.

Avalonia 데스크톱 앱은 향후 플랫폼 확장을 고려해 `net10.0` 공통 TFM을 유지한다. `.slnx`는 관리형 프로젝트만 빌드하지만, cross-publish 가능 여부만으로 해당 플랫폼을 지원한다고 간주하지 않는다. MVP의 빌드, 테스트, 완료 조건은 `win-x64`만 대상으로 한다.

## 4. 저장소 및 프로젝트 구성

현재 저장소에는 `net10.0` 대상의 `ICCProfileViewer.Core` 프로젝트가 있으므로 이를 순수 도메인/계산 계층으로 유지한다.

예정 구조는 다음과 같다.

```text
ICCProfileViewer.slnx
├─ build-lcms.cmd                  Windows용 수동 LittleCMS build
├─ ICCProfileViewer.App/
│  ├─ Views/
│  ├─ ViewModels/
│  ├─ Controls/
│  └─ Assets/
├─ ICCProfileViewer.Core/
│  ├─ Colorimetry/
│  ├─ Profiles/
│  └─ ReferenceGamuts/
├─ ICCProfileViewer.Lcms/
│  ├─ LcmsProfileReader.cs
│  ├─ LcmsTransformService.cs
│  └─ NativeLibraryBootstrapper.cs
├─ ICCProfileViewer.Core.Tests/
├─ ICCProfileViewer.Lcms.IntegrationTests/
├─ External/
│  └─ Little-CMS/                  Git submodule
├─ Artifacts/native/<rid>/<config>/ Git에서 제외된 빌드 결과
├─ TestData/Profiles/
├─ Docs/
│  ├─ implementation-plan.md
│  └─ native-build.md             MVP Windows script 및 향후 macOS/Linux 설치 매뉴얼
└─ Directory.Packages.props
```

### 4.1 의존성 방향

```text
ICCProfileViewer.App
  -> ICCProfileViewer.Core
  -> ICCProfileViewer.Lcms

ICCProfileViewer.Lcms
  -> ICCProfileViewer.Core
  -> lcmsNET

ICCProfileViewer.Core
  -> 외부 UI/네이티브 패키지에 의존하지 않음
```

`Core`에는 Avalonia 타입과 `lcmsNET` 타입을 노출하지 않는다. 이 규칙을 지키면 색도 계산 테스트가 쉬워지고, 필요할 때 UI 또는 ICC 바인딩을 교체할 수 있다.

`.slnx` 및 관리형 project에는 C/C++ build orchestration을 추가하지 않는다. native library 준비와 검증은 `Docs/native-build.md`에 정의한 별도 수동 절차로 관리한다.

## 5. 핵심 모델과 서비스

### 5.1 Core 모델

- `IccProfileInfo`
  - 버전, class, data color space, PCS, rendering intent
  - 설명, 제조사, 모델, 날짜
  - white/black point와 태그 요약
- `IccTagInfo`
  - signature, type, offset, size, 표시 가능한 값
- `XyzColor`
- `XyChromaticity`
- `UvPrimeChromaticity`
- `ChromaticityPoint`
  - 좌표, 라벨, 역할(원색/백색점/경계)
- `GamutBoundary`
  - 경계점, 계산 방법, 정확/근사 상태
- `ReferenceGamut`
  - 표준명, 원색, 백색점, 전달 함수 설명, 출처

### 5.2 서비스 경계

```csharp
public interface IIccProfileReader
{
    Task<IccProfileDocument> ReadAsync(
        Stream profileStream,
        string displayName,
        CancellationToken cancellationToken);
}

public interface IProfileGamutExtractor
{
    Task<GamutExtractionResult> ExtractAsync(
        IccProfileDocument profile,
        GamutExtractionOptions options,
        CancellationToken cancellationToken);
}
```

Avalonia의 Storage Provider에서 받은 파일은 먼저 스트림으로 처리한다. 플랫폼별 경로 문자열을 핵심 계층에 전달하지 않는다. `lcmsNET` API가 메모리/스트림 열기를 충분히 제공하지 않는 경우에만 앱 전용 임시 파일 어댑터를 사용하고 종료 시 정리한다.

## 6. lcmsNET 통합 계획

lcmsNET upstream 문서는 LittleCMS 2.9 이상을 실행 요구사항으로 명시한다. MVP에서는 Windows x64용 LittleCMS 안정 버전을 선택해 고정하고 검증한다. 이후 지원 플랫폼을 추가할 때도 같은 버전을 우선 사용한다.

### 6.1 사전 검증 스파이크

본 구현에 앞서 작은 통합 테스트로 다음을 확인한다.

1. `lcmsNET` 1.2.1을 `net10.0` 프로젝트에서 restore/build할 수 있는가.
2. 메모리 또는 스트림에서 프로필을 열 수 있는가.
3. v2/v4 버전, class, color space, PCS를 읽을 수 있는가.
4. `rXYZ`, `gXYZ`, `bXYZ`, `wtpt`, `bkpt`, `chad`, `desc`, `mluc` 태그를 필요한 형태로 읽을 수 있는가.
5. RGB 부동소수점 입력에서 XYZ 출력으로 transform을 생성할 수 있는가.
6. 오류 콜백의 수명과 스레드 동작이 안전한가.
7. 프로필/transform 객체의 `Dispose`가 네이티브 핸들을 정확히 해제하는가.
8. Windows에서 네이티브 라이브러리 이름과 경로를 바인딩이 어떻게 탐색하는가.

현재까지 확인한 결과는 다음과 같다.

- `lcmsNET` 1.2.1은 `net10.0` 프로젝트에서 restore와 build가 가능하다.
- `Profile.Open(byte[])`를 제공하므로 ICC profile stream을 임시 파일 없이 메모리에서 열 수 있다.
- `Profile`에서 ICC version, profile class, data color space, PCS, rendering intent, 생성 시각, 설명과 기본 tag count를 읽을 수 있다.
- `rXYZ`, `gXYZ`, `bXYZ`, `wtpt`, `bkpt`는 `CIEXYZ`로, `chad`는 `CIEXYZTRIPLE`로 typed read할 수 있다. `desc`와 `mluc` 기반 설명은 `GetProfileInfo`로 locale fallback을 적용해 읽을 수 있다.
- `TYPE_RGB_DBL` 입력과 `TYPE_XYZ_DBL` 출력 transform을 만들 수 있으며, Matrix/TRC fixture의 절대 색도 R/G/B 결과가 PCS colorant 값과 허용 오차 안에서 일치한다.
- 각 읽기/transform 작업에 전용 LittleCMS context와 오류 callback을 두면 전역 callback 없이 native 오류 코드와 영어 진단 문구를 수집할 수 있다. callback delegate는 context scope가 소유한다.
- context, profile, output profile, transform을 100회 반복 생성·해제하는 수명 smoke test가 통과한다.
- `lcmsNET`의 native import 이름은 `lcms2`이며, Windows x64에서는 `NativeLibrary.SetDllImportResolver`로 명시 경로와 앱 로컬 `lcms2.dll`을 우선 탐색할 수 있다.
- 별도 process test host에서 명시 경로, 앱 로컬 DLL, `PATH`를 통한 OS 기본 탐색, 존재하지 않는 명시 경로의 진단 오류를 서로 격리해 검증했다.
- LittleCMS 2.19에는 `cmsGetTagOffsetAndSize`가 추가되었지만 `lcmsNET` 1.2.1은 이를 노출하지 않고, 2.19 이전 LittleCMS에는 해당 symbol이 없다. 최소 2.9 호환성을 유지하기 위해 고정 ICC tag directory를 Core에서 사전 검사하여 tag signature, type signature, offset, size를 추출한다.
- submodule의 v2/v4 test profile을 MSTest 통합 테스트 fixture로 사용하고, 수동 빌드한 Little CMS 2.19 Windows x64 artifact로 검증한다.

단계 0 사전 검증 항목은 완료했다. 이후 기능 구현에서는 raw tag parser가 먼저 크기, `acsp`, tag count, offset/size 범위를 검사하고, LittleCMS가 tag 의미 해석과 color transform을 담당한다.

스파이크 결과 `lcmsNET`에 필요한 API가 일부 없을 경우 전체 바인딩을 교체하지 않고 다음 순서로 대응한다.

1. 가능한 기능은 `lcmsNET`으로 유지한다.
2. 누락된 소수의 LittleCMS 함수만 프로젝트 내부의 얇은 P/Invoke 어댑터로 보완한다.
3. 이 보완 계층도 `ICCProfileViewer.Lcms` 밖으로 노출하지 않는다.

### 6.2 Little-CMS Git submodule

Little-CMS 소스는 재현 가능한 수동 source build가 필요할 때 사용할 수 있도록 `External/Little-CMS`에 Git submodule로 포함한다. 현재 `lcms2.19.1`의 commit `21c582a594fe5279f90c0b93437c398f93bf62b0`에 고정한다.

```powershell
git submodule update --init --recursive
git -C External/Little-CMS rev-parse HEAD
```

정책은 다음과 같다.

- `master`를 따라가지 않고 검증한 release commit에 고정한다.
- 최초 기준은 문서 작성 시점의 안정판 `lcms2.19.1`이며 Windows x64 source build를 검증했다.
- 시스템에 설치된 LittleCMS를 사용할 개발자는 submodule을 초기화하지 않아도 된다.
- source build가 필요할 때만 `git submodule update --init --recursive`를 실행한다.
- 처음부터 source build를 할 clone에서는 `git clone --recurse-submodules`를 사용할 수 있다.
- submodule 갱신은 별도 변경으로 수행하고 모든 플랫폼 smoke test를 다시 실행한다.
- submodule 내부에 생성된 빌드 결과나 로컬 수정은 커밋하지 않는다.
- LittleCMS와 lcmsNET의 MIT 라이선스 고지를 배포물에 포함한다.

### 6.3 native library 사용 방식

애플리케이션은 다음 두 방식을 지원한다.

#### 시스템 설치본

- 운영체제 package manager나 사용자가 직접 설치한 LittleCMS를 사용한다.
- OS dynamic loader가 `lcms2`를 찾을 수 있어야 한다.
- 개발 환경에서 가장 간단한 방식이며, MVP 이후 Linux 배포에서도 우선 검토한다.
- 앱 시작 시 실제 로드된 LittleCMS version과 경로를 진단 로그에 남긴다.

#### 앱 로컬 library

- 개발자 또는 릴리스 담당자가 수동으로 빌드한 shared library를 실행 파일 옆 또는 문서에 지정한 native 경로에 둔다.
- 저장소에는 shared library를 커밋하지 않는다.
- 공개 배포물을 self-contained 형태로 제공할 경우 이 방식으로 native library를 publish 결과에 수동으로 포함한다.

`NativeLibraryBootstrapper`의 탐색 순서는 다음을 목표로 한다.

1. 명시적으로 지정한 `ICC_PROFILE_VIEWER_LCMS_PATH`
2. 실행 파일과 같은 디렉터리의 앱 로컬 library
3. OS 기본 dynamic-library 탐색 경로의 시스템 설치본

이 탐색 순서를 `lcmsNET` assembly에 적용할 수 있는지는 단계 0에서 `NativeLibrary.SetDllImportResolver`와 실제 package import 이름으로 검증한다. resolver 적용이 어렵다면 lcmsNET의 기본 import 규칙에 맞춰 앱 로컬 파일명과 설치 경로를 제한한다.

### 6.4 플랫폼별 수동 설치 및 빌드

#### Windows

저장소 루트에 `build-lcms.cmd`를 제공한다. 이 script는 `.slnx`에서 자동 호출하지 않으며, 개발자나 릴리스 담당자가 Visual Studio Developer Command Prompt 또는 Developer PowerShell에서 명시적으로 실행한다.

예상 사용법은 다음과 같다.

```powershell
.\build-lcms.cmd
.\build-lcms.cmd Release x64
```

MVP에서는 x64만 지원하며 기본값은 `Release x64`로 한다. script의 역할은 다음과 같다.

1. `External\Little-CMS` submodule과 `Projects\VC*` 디렉터리를 확인한다.
2. Visual Studio Installer에 포함된 `vswhere.exe`와 component ID `Microsoft.VisualStudio.Component.VC.Tools.x86.x64`로 C++ toolset이 설치된 모든 Visual Studio/Build Tools instance를 조회한다.
3. 설치 version이 가장 높은 instance부터 검사한다.
4. 해당 instance의 product line과 일치하는 LittleCMS solution 디렉터리(`VC2026`, `VC2022`, `VC2019`)가 있는 첫 조합을 선택한다.
5. 각 solution에 고정된 toolset(`v145`, `v143`, `v142`)을 사용하고 임의 retarget은 하지 않는다.
6. Developer Command Prompt 또는 Developer PowerShell에서 실행되었는지 확인한다.
7. LittleCMS solution의 `lcms2_DLL` target만 MSBuild로 빌드한다.
8. 결과 DLL과 선택적 LIB/PDB를 `Artifacts\native\<rid>\<Configuration>\`에 복사하고 commit, compiler, solution 정보를 `build-info.txt`에 기록한다.

탐색의 기준이 되는 `vswhere` 호출은 다음 형태로 한다.

```bat
"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" ^
  -latest -prerelease -products * ^
  -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 ^
  -version "[18.0,19.0)" ^
  -property installationPath
```

script는 다음 원칙을 따른다.

- 현재 Developer Command Prompt 환경보다 더 높은 호환 Visual Studio/VC++ 설치가 있으면 높은 설치를 선택한다.
- `vswhere`를 찾지 못하면 자동 선택을 중단하고 Visual Studio Installer와 C++ workload 설치 방법을 안내한다.
- 설치된 Visual Studio와 일치하는 `Projects\VCyyyy` 디렉터리가 없으면 임의로 낮은 solution을 빌드하지 않고, 발견한 설치/디렉터리 목록과 해결 방법을 출력한다.
- `.sln` 파일명과 DLL project/output 경로는 고정한 submodule revision을 기준으로 script에 명시한다. runtime glob으로 우연한 파일을 선택하지 않는다.
- Visual Studio 또는 C++ Desktop workload를 자동 설치하지 않는다.
- 실패 시 원래 MSBuild exit code를 반환한다.
- submodule을 자동으로 clone/update하지 않고 필요한 `git submodule update --init --recursive` 명령을 안내한다.

Developer PowerShell에서도 `.cmd` 파일을 호출할 수 있다. Developer Command Prompt 또는 Developer PowerShell에서 실행한 경우에도 같은 탐색·선택 규칙을 적용한다.

source build 대신 별도로 설치된 `lcms2.dll`을 loader 탐색 경로나 앱 실행 파일 옆에 두는 방식도 계속 지원한다.

#### MVP 이후: macOS와 Linux

macOS와 Linux 지원을 구현할 때는 source build보다 운영체제 package manager 설치를 기본 권장 경로로 한다. 아래 내용은 MVP 완료 조건이나 MVP 테스트 대상에 포함하지 않는다.

macOS:

```bash
brew install little-cms2
```

Linux의 대표적인 package 이름은 다음과 같다. 실제 명령과 지원 version은 `Docs/native-build.md`에서 지원 배포판별로 검증한다.

```bash
# Debian / Ubuntu
sudo apt install liblcms2-2

# Fedora
sudo dnf install lcms2

# Arch Linux
sudo pacman -S lcms2
```

배포판 runtime package가 versioned soname만 제공하고 `lcmsNET`의 기본 import 이름으로 찾지 못하는 경우에는 다음 순서로 대응한다.

1. `NativeLibraryBootstrapper`에서 해당 soname을 명시적으로 탐색한다.
2. 그래도 해결되지 않을 때만 development package(`liblcms2-dev`, `lcms2-devel` 등)를 설치하도록 안내한다.
3. 임의의 symlink를 시스템 디렉터리에 만들도록 안내하지 않는다.

package manager를 사용할 수 없거나 특정 LittleCMS version이 필요할 때는 다음 source-build 경로를 fallback으로 제공한다.

1. submodule 루트의 `meson.build`로 shared library를 수동 빌드한다.
2. CMake 또는 Unix reference 경로인 Autotools로 수동 빌드한다.

```text
meson setup Artifacts/native/<rid>/<config>/build External/Little-CMS <shared-library options>
meson compile -C Artifacts/native/<rid>/<config>/build
```

upstream은 현재 Meson 지원을 “testing”으로 표시한다. Meson에서 문제가 발생하면 CMake 또는 Autotools를 사용한다. 앱에는 core `lcms2` shared library만 필요하므로 source build에서는 JPEG/TIFF utilities와 불필요한 도구를 끈 최소 구성을 사용한다.

`Docs/native-build.md`에는 우선 `build-lcms.cmd` 사용법, submodule 초기화, 예상 출력 파일, 앱 로컬 복사 위치, 진단 명령을 기록한다. macOS와 Linux의 package 설치 및 source-build 절차는 해당 플랫폼 지원을 시작할 때 검증해 추가한다. `.slnx` build는 이 script나 package manager를 실행하지 않는다.

### 6.5 수동 artifact와 배포 정책

수동 source build 결과를 보관해야 할 때는 Git-ignored artifacts 영역을 사용한다.

```text
Artifacts/native/
└─ win-x64/Release/lcms2.dll
```

- 실제 파일명과 soname은 `lcmsNET`의 import 이름 및 각 플랫폼 로더 동작을 단계 0에서 확인한다.
- submodule commit, compiler, build system 버전, build option을 artifact metadata에 기록한다.
- submodule revision을 변경하면 보안 공지와 release note를 확인한다.
- native library를 변경하면 Windows x64 integration/smoke test를 다시 실행한다.
- prebuilt shared library를 Git에 커밋하지 않는다.

MVP 이후 macOS와 Linux를 지원할 때는 RID별 artifact 디렉터리를 추가한다. macOS 앱 로컬 dylib는 `.app` bundle 위치와 서명을, Linux 시스템 설치본은 `ldconfig`/`ldd`, 앱 로컬 library는 `rpath`와 실제 로드 경로를 확인한다.

## 7. ICC 처리 정책

### 7.1 파일 검증

ICC 파일은 신뢰할 수 없는 바이너리 입력으로 취급한다.

- 기본 최대 파일 크기를 64 MiB로 제한한다.
- 최소 헤더 길이와 `acsp` signature를 확인한다.
- 헤더의 선언 크기가 실제 파일보다 크면 잘린 입력으로 거부한다. 선언 크기 뒤의 trailing data는 ICC 데이터에서 제외하되, 선언 크기를 tag offset/size의 검증 경계로 유지한다.
- 비정상 tag count, offset, size를 사전 검사한다.
- LittleCMS 오류를 사용자 친화적인 도메인 오류로 변환한다.
- 파싱은 UI 스레드 밖에서 수행한다.
- 새 파일을 열면 이전 파싱/계산 작업을 취소한다.

향후 실제 크래시 격리가 필요할 정도로 악성 파일 지원 범위가 커지면 별도 helper process에서 파싱하는 방안을 검토한다.

### 7.2 프로필 종류별 표시

| 프로필 형태 | 메타데이터 | MVP gamut 표시 |
|---|---|---|
| RGB Matrix/TRC | 지원 | 정확한 원색 삼각형 |
| RGB LUT | 지원 | 2D gamut 미지원 안내 |
| CMYK LUT | 지원 | 단순 삼각형을 그리지 않음 |
| Gray | 지원 | 백색점/중성축 정보만 표시 |
| Lab/XYZ | 지원 | data color space 정보만 표시 |
| N-channel/Named Color | 가능한 범위에서 지원 | 2D gamut 미지원 안내 |

## 8. 색도 계산

### 8.1 기본 변환식

`XYZ -> xy`:

```text
s = X + Y + Z
x = X / s
y = Y / s
```

`XYZ -> CIE 1976 u'v'`:

```text
d = X + 15Y + 3Z
u' = 4X / d
v' = 9Y / d
```

분모가 0에 가까운 경우 결과 없음으로 처리한다. 계산에는 `double`을 사용한다.

### 8.2 Matrix/TRC RGB 프로필

RGB Matrix/TRC 프로필은 다음 원칙으로 처리한다.

1. 프로필의 RGB 원색 및 백색점 태그를 읽는다.
2. ICC PCS의 D50 기준과 프로필의 device-side 백색점 사이의 chromatic adaptation을 고려한다.
3. `chad`를 무시한 단순 XYZ-to-xy 변환을 하지 않는다.
4. 가능하면 LittleCMS의 절대 색도 변환 경로로 R/G/B 원색과 백색점을 XYZ로 얻는다.
5. 태그 직접 계산 경로는 동일 결과를 내는지 테스트 fixture로 검증한다.
6. 계산 경로와 적용한 adaptation을 결과 메타데이터에 기록한다.

## 9. 기준 색역 데이터

기준 색역은 코드에 상수로 관리하되, 좌표와 출처를 테스트에서도 고정한다.

| 색역 | Red xy | Green xy | Blue xy | White xy |
|---|---:|---:|---:|---:|
| sRGB | 0.6400, 0.3300 | 0.3000, 0.6000 | 0.1500, 0.0600 | D65: 0.3127, 0.3290 |
| Display P3 | 0.6800, 0.3200 | 0.2650, 0.6900 | 0.1500, 0.0600 | D65: 0.3127, 0.3290 |
| DCI-P3 | 0.6800, 0.3200 | 0.2650, 0.6900 | 0.1500, 0.0600 | DCI: 0.3140, 0.3510 |
| Adobe RGB (1998) | 0.6400, 0.3300 | 0.2100, 0.7100 | 0.1500, 0.0600 | D65: 0.3127, 0.3290 |
| BT.2020 | 0.7080, 0.2920 | 0.1700, 0.7970 | 0.1310, 0.0460 | D65: 0.3127, 0.3290 |

UI 명칭은 다음 정책을 따른다.

- 일반 디스플레이 비교 항목은 `Display P3`로 표시한다.
- 디지털 시네마 기준은 `DCI-P3 (DCI white)`로 표시한다.
- `P3-D65`라는 모호한 단독 명칭은 사용하지 않는다.
- Display P3와 DCI-P3 D65는 2차원 원색 삼각형과 백색점이 같지만 전달 함수가 다르다는 설명을 도움말에 제공한다.

## 10. 다이어그램 렌더링

### 10.1 구성

`ChromaticityDiagramControl`을 Avalonia 커스텀 컨트롤로 구현한다.

렌더링 순서는 다음과 같다.

1. 캐시된 색도 배경 bitmap
2. spectral locus와 purple line
3. 축, 눈금, 격자
4. 기준 색역 경계
5. 선택 프로필 경계
6. 원색점과 백색점
7. 라벨과 hover tooltip

벡터 요소는 Avalonia `DrawingContext`/geometry를 사용한다. 색도 배경은 크기와 diagram type별로 캐시한 bitmap을 사용하여 창 크기 변경 중의 반복 계산을 줄인다. 직접적인 SkiaSharp API 의존은 MVP에서 추가하지 않는다.

### 10.2 표시 정책

- 프로필 경계는 가장 굵은 실선으로 표시한다.
- 기준 색역은 색상과 dash pattern을 함께 달리하여 색각 이상 및 흑백 캡처에서도 구분한다.
- 체크박스와 선 스타일을 범례에서 동일하게 표시한다.
- `xy`와 `u'v'` 전환 시 같은 선택 상태를 유지한다.
- 백색점 표시는 개별 토글로 제공한다.
- 배경색은 현재 모니터의 출력 한계 때문에 색도값을 정확하게 재현하지 못한다는 안내를 제공한다.

## 11. UI 구성

메인 화면은 다음 영역으로 구성한다.

```text
┌─────────────────────────────────────────────────────────────┐
│ Open Profile    file.icc                    상태/오류        │
├──────────────────────┬──────────────────────────────────────┤
│ Profile Summary      │ [CIE 1931 xy] [CIE 1976 u'v']       │
│ - Version            │                                      │
│ - Class              │          Chromaticity Diagram        │
│ - Color space / PCS  │                                      │
│ - Description        │                                      │
│ - White point        │                                      │
├──────────────────────┼──────────────────────────────────────┤
│ Tags                 │ Overlays: sRGB / P3 / Adobe / 2020  │
└──────────────────────┴──────────────────────────────────────┘
```

필수 상호작용은 다음과 같다.

- 파일 선택
- `.icc`, `.icm` drag-and-drop
- 기준 색역 toggle
- `xy`/`u'v'` 전환
- diagram 좌표 tooltip
- 원색/백색점 좌표 복사
- 지원하지 않는 gamut 유형에 대한 설명

## 12. 상태 및 오류 처리

ViewModel은 다음 상태를 명시적으로 갖는다.

- `Empty`: 파일을 열기 전
- `Loading`: 프로필 파싱 중
- `Loaded`: 메타데이터와 gamut 준비 완료
- `PartiallySupported`: 메타데이터는 표시되지만 gamut은 미지원
- `InvalidProfile`: 손상되거나 ICC가 아닌 파일
- `NativeDependencyError`: lcms2 로드 실패
- `UnexpectedError`: 예상하지 못한 오류

특히 네이티브 라이브러리 누락은 일반적인 “파일을 열 수 없음”과 구분해 버전, RID, 탐색 위치를 진단 로그에 남긴다.

LittleCMS가 설치되지 않았더라도 프로세스가 시작 단계에서 종료되어서는 안 된다. `NativeLibraryBootstrapper.TryInitialize` 형태로 native dependency를 먼저 검사하고, 실패하면 프로필 열기 기능을 비활성화한 상태에서 `Docs/native-build.md`에 대응하는 설치·앱 로컬 복사 안내를 표시한다.

## 13. 테스트 전략

### 13.1 Core 단위 테스트

- XYZ-to-xy 변환
- XYZ-to-u'v' 변환
- 0 분모 처리
- `xy <-> u'v'` 기준값
- 모든 기준 색역의 좌표
- 좌표계와 화면 좌표 간 변환
- polygon clipping과 hit testing

### 13.2 LittleCMS 통합 테스트

- 공식/검증된 sRGB v2 프로필
- sRGB v4 프로필
- Adobe RGB Matrix/TRC 프로필
- Display P3 프로필
- RGB LUT 프로필
- CMYK 프로필
- Gray 프로필
- 손상된 헤더와 tag offset을 가진 프로필
- Windows 10/11 x64에서 검증한 프로필의 핵심 메타데이터 확인
- 시스템 설치본에서 실제 로드된 LittleCMS version과 경로 확인
- 앱 로컬 library를 사용할 때 시스템 설치본 없이도 로드되는지 확인
- native library가 없을 때 설치/복사 방법을 포함한 진단 오류가 표시되는지 확인
- 프로필과 transform을 반복 생성/해제하는 누수 smoke test

Windows native-build script는 별도로 다음을 검증한다.

- Developer Command Prompt와 Developer PowerShell 양쪽에서 실행
- Visual Studio IDE와 Build Tools instance 탐색
- 여러 Visual Studio version이 있을 때 가장 높은 호환 `VCyyyy` 조합 선택
- x64 및 Debug/Release 인자 검증
- submodule/toolset/solution 누락 시 명확한 오류와 non-zero exit code
- 성공 시 예상 DLL과 build metadata가 `Artifacts/native`에 생성되는지 확인

테스트 데이터는 라이선스를 확인한 뒤 저장소에 포함하거나, 테스트 시 공식 출처에서 검증된 해시의 파일을 준비한다. 네트워크가 없어도 기본 테스트가 실행될 수 있도록 최소 fixture는 저장소에 포함하는 쪽을 우선한다.

### 13.3 UI 테스트

- ViewModel 상태 전환
- overlay 선택 유지
- 잘못된 파일 오류 표시
- LUT/CMYK의 부분 지원 메시지
- 크기 변경 및 고해상도 DPI에서의 diagram 배치

### 13.4 수동 플랫폼 검증

- Windows x64에서 open, drag-and-drop, publish 실행
- Windows 10/11에서 clean machine 실행과 의존성 확인
- Windows의 100%, 150%, 200% 스케일 확인
- 다크/라이트 테마 확인

Windows ARM64, macOS, Linux 검증은 MVP 완료 조건에 포함하지 않으며 각 플랫폼 지원 단계에서 추가한다.

## 14. 빌드와 배포

`.slnx` build는 관리형 Avalonia 및 C# 프로젝트만 빌드한다. LittleCMS 설치, source build, 파일 복사는 자동으로 수행하지 않는다.

기본 관리형 build는 다음과 같다.

```powershell
dotnet restore ICCProfileViewer.slnx
dotnet build ICCProfileViewer.slnx -c Debug
```

이 build는 native library가 없어도 compile까지 완료될 수 있다. 애플리케이션 실행과 LittleCMS 통합 테스트에는 시스템 설치본 또는 앱 로컬 library가 필요하다. `--self-contained true`는 .NET runtime을 포함한다는 뜻이며 LittleCMS까지 자동으로 포함한다는 뜻은 아니다.

예상 publish 명령은 다음과 같다.

```powershell
dotnet publish ICCProfileViewer.App -c Release -r win-x64 --self-contained true
```

publish 방식은 다음 두 가지 중 릴리스 정책으로 선택한다.

- 시스템 의존형: native library를 포함하지 않고 설치 요구사항을 배포 문서에 명시한다.
- 앱 로컬형: 릴리스 담당자가 미리 준비한 native library를 publish 결과에 수동 복사한다.

MVP 앱 로컬형 공개 배포물은 Windows x64 환경에서 native artifact를 준비하고 실제 실행을 검증한 뒤 최종 패키징한다.

배포 형태는 다음을 목표로 한다.

- Windows x64: zip 우선, 이후 MSIX 또는 installer 검토

macOS와 Linux의 publish 명령 및 패키징 형식은 MVP 이후 해당 플랫폼을 실제로 지원하고 테스트할 때 확정한다.

Native AOT와 trimming은 첫 릴리스 범위에서 제외한다. `lcmsNET`, reflection, Avalonia XAML 및 네이티브 로딩이 모두 검증된 뒤 별도 최적화 작업으로 수행한다.

## 15. 구현 단계

### 단계 0: lcmsNET/네이티브 스파이크

상태: 완료 (2026-07-19)

- Little-CMS를 `External/Little-CMS` submodule로 추가하고 release commit 고정
- `lcmsNET` 패키지 참조
- Windows x64에서 v2/v4 프로필 open
- 핵심 메타데이터와 태그 읽기
- RGB-to-XYZ transform 검증
- Windows VC2026 solution의 정확한 project/configuration/output 확인
- `vswhere`/Developer environment를 이용한 Windows compiler 및 `VCyyyy` 선택 규칙 확인
- 시스템 설치본과 앱 로컬 library의 탐색 순서 검증
- native library 누락 시 진단 오류 검증
- `Docs/native-build.md`의 Windows 수동 source build 절차 검증

완료 조건: Windows x64의 시스템 설치 및 수동 source build 방법, native 탐색 순서와 필요한 API 사용법이 문서화되고 Windows 통합 테스트가 통과한다.

### 단계 1: 솔루션 골격

상태: 진행 중. Avalonia 12.1.0 App과 기본 MVVM 화면, native dependency 상태 probe, App ViewModel MSTest까지 구현했다. 기본 CI build/test 구성은 남아 있다.

- Avalonia App 프로젝트 생성
- Lcms 어댑터와 테스트 프로젝트 생성
- 저장소 루트 `build-lcms.cmd` 구현
- `Docs/native-build.md`에 Windows script와 수동 설치 절차 작성
- Central Package Management 도입
- 의존성 방향 검증
- 기본 CI build/test 구성

### 단계 2: 프로필 메타데이터

상태: 진행 중. Storage Provider 기반 `.icc`/`.icm` 파일 선택, 취소 가능한 비동기 파싱, v2/v4 메타데이터와 tag 목록 표시, 손상된 프로필 및 native dependency 오류 상태를 구현했다. drag-and-drop과 별도 진단 로그는 남아 있다.

- 파일 선택 및 drag-and-drop
- 비동기 파싱과 취소
- v2/v4 메타데이터 표시
- tag 목록
- 오류 상태 및 로그

### 단계 3: 색도 계산 엔진

- 색도 좌표 타입과 변환식
- 기준 색역 정의
- spectral locus 데이터
- Matrix/TRC gamut 추출
- D50/device white adaptation 검증

### 단계 4: 다이어그램 UI

- `xy` 다이어그램
- `u'v'` 다이어그램
- overlay, 범례, tooltip
- DPI와 테마 대응
- 렌더링 캐시

### 단계 5: Windows x64 배포

- 시스템 의존형과 앱 로컬형 배포 정책 확정
- 앱 로컬형이면 Windows x64 native artifact를 수동 준비하고 publish 결과에 복사
- Windows x64 self-contained publish
- Windows 10/11 x64 clean machine 검증
- 라이선스 및 ThirdPartyNotices 포함

### 단계 6: MVP 이후 플랫폼 확장

- Windows ARM64, macOS, Linux의 architecture와 RID 범위 확정
- 각 플랫폼의 LittleCMS 설치, 앱 로컬 배치, resolver 동작 검증
- 대상 플랫폼에서 build, integration test, UI 수동 검증 수행
- 플랫폼별 publish와 패키징 절차 구현
- 실제 테스트를 통과한 플랫폼만 공식 지원 목록에 추가

### 단계 7: 아주 장기 후속 CLI

- 별도 `ICCProfileViewer.Cli` 프로젝트 생성
- CIE 1931 `xy` 및 CIE 1976 `u'v'` diagram의 PNG/SVG 출력
- 데스크톱 앱과 Core 계산·기준 색역 정의·렌더링 설정 공유
- CLI 구현 시점에 공식 지원하는 플랫폼에서 headless 실행 및 파일 출력 검증

이 단계는 MVP 완료 직후의 우선 작업이 아니라 장기 후속 과제로 둔다.

## 16. MVP 완료 조건

다음 조건을 모두 만족하면 MVP를 완료한 것으로 본다.

1. Windows 10/11 x64에서 앱이 실행된다.
2. Windows x64에서 시스템 설치본 또는 앱 로컬 LittleCMS를 로드할 수 있으며, 누락 시 해결 방법이 포함된 오류를 표시한다.
3. 검증된 ICC v2/v4 프로필의 메타데이터가 표시된다.
4. RGB Matrix/TRC 프로필의 `xy`, `u'v'` 경계가 기준 fixture와 허용 오차 내에서 일치한다.
5. 5개 기준 색역을 독립적으로 중첩할 수 있다.
6. LUT/CMYK 등 미지원 gamut을 잘못된 삼각형으로 표시하지 않는다.
7. 잘못된 ICC 파일이 앱을 종료시키지 않고 오류 상태로 처리된다.
8. Core 단위 테스트와 Windows x64 LittleCMS smoke test가 통과한다.
9. 배포물에 필요한 오픈소스 라이선스 고지가 포함된다.

## 17. 주요 위험과 대응

| 위험 | 영향 | 대응 |
|---|---|---|
| native library 미설치 | 앱 시작 또는 ICC 기능 실패 | 탐색 경로와 설치/복사 방법을 포함한 진단 오류 제공 |
| 시스템 LittleCMS 버전 차이 | 플랫폼별 동작 차이 | 시작 시 version/path 기록, 지원 최소 버전 검사, release 테스트 버전 명시 |
| 수동 앱 로컬 복사 누락 | 배포물에서 `DllNotFoundException` | 릴리스 체크리스트와 clean-machine smoke test 수행 |
| Windows compiler와 upstream `VCyyyy` 불일치 | 잘못된 solution 선택 또는 build 실패 | `vswhere` 결과와 실제 디렉터리의 교집합만 사용하고 임의 downgrade 금지 |
| MVP 이후 Linux runtime package의 versioned soname | lcmsNET import 실패 | Linux 지원 단계에서 resolver의 soname 지원을 검증하고 필요할 때만 development package 안내 |
| MVP 이후 Meson upstream 지원이 testing 상태 | macOS/Linux build 회귀 | 플랫폼 확장 단계에서 CMake/Autotools fallback을 유지하고 revision 갱신 시 재검증 |
| 플랫폼별 라이브러리 이름/로더 차이 | `DllNotFoundException` | 초기 스파이크에서 resolver와 실제 publish 결과 검증 |
| ICC PCS D50 값을 D65 기준 색역과 직접 비교 | 잘못된 삼각형 | adaptation 정책을 Core에 명시하고 공식 fixture로 검증 |
| LUT/CMYK를 RGB 삼각형처럼 취급 | 오해를 유발하는 결과 | 지원하지 않는 2D gamut임을 명확히 표시하고 경계를 그리지 않음 |
| 손상된 ICC 입력 | 예외, 메모리 문제, 네이티브 크래시 | 크기/offset 사전 검사, 최신 lcms2 고정, 필요 시 프로세스 격리 |
| MVP 이후 macOS signing/notarization | 배포 차단 | macOS 지원 단계에서 실제 macOS CI 또는 장비로 릴리스 파이프라인 검증 |
| MVP 이후 Linux 배포판 차이 | 실행 환경별 실패 | Linux 지원 단계에서 대상 범위를 명시하고 clean VM 및 패키징 방식을 검증 |

## 18. 구현 전에 확정할 세부 결정

다음 항목은 단계 0 결과를 바탕으로 확정한다.

1. `lcmsNET`만으로 태그 읽기와 transform 요구사항을 모두 충족하는지
2. Windows VC2026 solution의 정확한 configuration/platform/output mapping
3. `build-lcms.cmd`가 지원할 Visual Studio product line 범위
4. MVP 이후 macOS/Linux fallback source-build 매뉴얼에서 Meson과 CMake 중 무엇을 우선 안내할지
5. `lcmsNET`이 요구하는 플랫폼별 import 이름과 output rename/resolver 정책
6. 공개 배포물을 시스템 의존형으로 제공할지 앱 로컬 library를 포함할지
7. MVP 이후 macOS에서 지원할 architecture 범위
8. MVP 이후 Linux에서 지원할 배포판과 architecture 범위
9. spectral locus/CMF 데이터의 저장 형식과 배포 라이선스
10. MVP 색도 배경을 컬러로 채울지, 정확도를 우선하여 중성 배경과 경계선만 제공할지

## 19. 참고 자료

- [Avalonia 12 breaking changes와 .NET 10 권장 대상](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Avalonia 지원 플랫폼](https://docs.avaloniaui.net/docs/supported-platforms)
- [Avalonia macOS 동작 및 배포 모델](https://docs.avaloniaui.net/docs/platform-specific-guides/macos)
- [Avalonia 네이티브 라이브러리 배치](https://docs.avaloniaui.net/docs/app-development/native-interop)
- [lcmsNET NuGet 패키지](https://www.nuget.org/packages/lcmsNET/)
- [lcmsNET 소스 및 사용 조건](https://github.com/jrshoare/lcmsNET)
- [LittleCMS 공식 사이트](https://www.littlecms.com/)
- [Little-CMS source repository](https://github.com/mm2/Little-CMS)
- [Little-CMS upstream build instructions](https://github.com/mm2/Little-CMS/blob/master/BUILDING.md)
- [Little-CMS Meson build definition](https://github.com/mm2/Little-CMS/blob/master/meson.build)
- [Microsoft Visual Studio instance detection (`vswhere`)](https://learn.microsoft.com/en-us/visualstudio/install/tools-for-managing-visual-studio-instances)
- [Microsoft Visual Studio Developer shells](https://learn.microsoft.com/en-us/visualstudio/ide/reference/command-prompt-powershell)
- [Homebrew `little-cms2` formula](https://formulae.brew.sh/formula/little-cms2)
- [Debian `liblcms2-2` package](https://packages.debian.org/bookworm/liblcms2-2)
- [Fedora `lcms2` package](https://packages.fedoraproject.org/pkgs/lcms2/lcms2/)
- [ICC v2/v4 사양](https://www.color.org/icc_specs2.xalter)
- [ICC sRGB 정의](https://registry.color.org/rgb-registry/srgb)
- [ICC Display P3 정의](https://registry.color.org/rgb-registry/displayp3)
- [ICC DCI-P3 정의](https://registry.color.org/rgb-registry/dcip3)
- [Adobe RGB (1998) 정의](https://www.adobe.com/digitalimag/pdfs/AdobeRGB1998.pdf)
- [ICC BT.2020 정의](https://registry.color.org/rgb-registry/bt2020)
