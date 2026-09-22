# NaverMapSearch

Flow Launcher에서 네이버 지역 검색 API로 장소를 찾는 C# 플러그인입니다.
**각 사용자가 본인의 Client ID와 Client Secret을 발급받아 설정합니다.**
공용 인증값이나 별도의 중계 서버는 사용하지 않습니다.

```text
nm 강남 카페
nm 판교 도서관
nm 서울시청
```

장소명·분류·주소를 목록으로 표시합니다. Enter를 누르면 기본 브라우저에서
해당 장소의 주소와 이름으로 네이버 지도를 검색합니다. API에 장소 ID가 없으므로
특정 장소의 상세 페이지로 바로 이동하는 방식은 아닙니다.
주소는 도로명 주소를 우선하며, 없으면 지번 주소를 표시합니다.

## 요구 사항과 설치

- Windows 10/11, .NET 9 기반 Flow Launcher 2.x (로컬 검증 대상: 2.1.4).
- 개발에는 .NET 9 SDK가 필요합니다. 일반 사용자는 SDK를 설치할 필요가 없습니다.
- 공식 플러그인 목록에는 아직 등록하지 않았습니다.

1. 아래 빌드 절차 또는 GitHub Actions의 `NaverMapSearch` 아티팩트에서
   `NaverMapSearch-1.0.0.zip`을 준비합니다. Actions 다운로드 ZIP 안에 플러그인 ZIP이 들어 있습니다.
2. Flow Launcher 설정에서 사용자 데이터 폴더를 열고 `Plugins/NaverMapSearch-1.0.0` 폴더에
   플러그인 ZIP을 풉니다. 일반 설치 경로는 `%APPDATA%/FlowLauncher/Plugins`입니다.
   포터블 설치는 해당 인스턴스의 사용자 데이터 폴더를 사용하세요.
3. 폴더 바로 아래에 `plugin.json`과 `Flow.Launcher.Plugin.NaverMapSearch.dll`이 있어야 합니다.
4. Flow Launcher를 다시 시작하고 설정 → 플러그인 → **NaverMapSearch**를 엽니다.

업데이트 시 Flow Launcher를 종료하고 이전 플러그인 폴더를 새 패키지로 교체하세요.
동일 플러그인을 여러 버전의 폴더에 동시에 두지 마세요.
인증값은 플러그인 설치 폴더와 별도인 사용자 설정 폴더에 저장됩니다.

## 인증값 발급과 설정

1. [네이버 개발자 센터](https://developers.naver.com/apps/#/register)에 로그인하고 애플리케이션을 등록합니다.
2. 사용 API에서 **검색**을 선택합니다. 네이버 지도 클라우드 API 인증값과는 다릅니다.
3. 발급된 **Client ID**, **Client Secret**을 플러그인 설정의
   `X-Naver-Client-Id`, `X-Naver-Client-Secret` 입력란에 각각 입력합니다.
4. **저장**을 누른 뒤 `nm 강남 카페`로 검색합니다. 저장은 로컬 저장만 수행하며,
   인증과 API 사용 권한 확인은 실제 검색 요청에서 이루어집니다.

Secret 입력란은 마스킹됩니다. 두 값 모두 Windows DPAPI(CurrentUser)로 암호화하여
Flow Launcher가 제공하는 플러그인 설정 폴더의 `credentials.json`에 저장합니다.
다른 PC/Windows 계정으로 복사했거나 파일이 손상되면 다시 입력하세요.
DPAPI는 같은 Windows 계정으로 실행되는 프로그램으로부터 비밀을 격리하는 수단은 아닙니다.
플러그인은 인증값을 로그에 기록하지 않으며 검색 시 네이버 HTTPS API에만 전송합니다.

## 검색 제한과 오류

- [지역 검색 API](https://developers.naver.com/docs/serviceapi/search/local/local.md)의
  제한에 따라 최대 **5개**, 정확도순(`sort=random`)으로 표시합니다. 페이지 이동은 지원하지 않습니다.
- `telephone`은 API가 값을 제공하지 않으므로 표시하지 않습니다.
- 300ms 입력 지연과 이전 요청 취소로 불필요한 호출을 줄입니다. 제한 시간은 10초입니다.
- 인증/권한 오류: 두 인증값과 네이버 애플리케이션의 검색 API 선택 여부를 확인하세요.
- 호출 한도 오류: 네이버 개발자 센터의 사용량을 확인하고 나중에 다시 시도하세요.
  동일 Client ID를 사용하는 다른 프로그램과 사용량이 합산됩니다.
- 네트워크·시간 초과·서버 오류: 연결을 확인하거나 잠시 후 다시 검색하세요.
- 결과가 없으면 지역명과 장소명을 더 구체적으로 입력하세요.

## 개발·테스트·패키징

```powershell
dotnet restore NaverMapSearch.sln
dotnet build NaverMapSearch.sln -c Release --no-restore
dotnet test NaverMapSearch.sln -c Release --no-build --no-restore
./scripts/Package.ps1
```

출력: `.artifacts/NaverMapSearch-1.0.0.zip`. 프로젝트 로컬 SDK를 사용하는 경우:

```powershell
./scripts/Package.ps1 -Dotnet "$PWD/.tools/dotnet/dotnet.exe"
```

아이콘은 독자 제작한 벡터 도형이며 `scripts/New-Icon.ps1`로 재생성할 수 있습니다.
테스트는 모의 HTTP 응답, 실제 DPAPI와 임시 설정 파일을 사용하며 네이버 인증값이 필요하지 않습니다.
실제 API 호출과 브라우저 동작은 별도 수동 확인 대상입니다.

GitHub Actions는 Windows에서 빌드·테스트 후 ZIP을 아티팩트로 저장합니다.
버전 태그 `v1.0.0`의 숫자는 프로젝트와 `plugin.json`의 버전과 일치해야 합니다.
워크플로는 GitHub Release 게시나 Flow Launcher 플러그인 목록 등록을 수행하지 않습니다.

## 라이선스

MIT. 네이버 공식 제품이 아니며 NAVER 상표·로고를 포함하지 않습니다.
