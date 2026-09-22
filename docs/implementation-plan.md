# NaverMapSearch 구현 계획

승인된 사양: C#/.NET 9, Flow.Launcher.Plugin 5.3.1, nm 검색, 최대 5건,
Enter로 네이버 지도 검색, WPF 인증값 설정, CurrentUser DPAPI 저장.

1. 모의 HTTP 기반 검색 클라이언트와 결과 변환 테스트 후 구현.
2. 실제 DPAPI와 임시 파일 기반 설정 저장 테스트 후 구현.
3. 취소/입력 지연/설정 변경 테스트 후 비동기 플러그인 및 WPF 연결.
4. 메타데이터·아이콘·문서·Windows CI·ZIP 패키징.
5. Release 빌드, 자동 테스트, 배포 파일 검증 및 가능한 로컬 실행 검증.

기준: 정확도순, 300ms debounce, 10초 timeout, 오류의 한국어 안내,
인증값과 검색 내용은 로그에 남기지 않음. 실제 키 없이 실 API 검증을 주장하지 않음.

작업 결정: 커밋이 없는 빈 저장소이므로 worktree 생성 대신 현재 폴더의
codex/naver-map-search 브랜치를 사용한다. SDK는 .tools에 격리한다.
공개 게시와 공식 목록 등록은 수행하지 않는다.
