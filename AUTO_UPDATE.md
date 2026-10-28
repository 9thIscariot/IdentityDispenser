# 인격 자판기 자동 업데이트 배포

Windows ZIP 배포용입니다. 앱을 실행하면 공개 GitHub 저장소
`9thIscariot/IdentityDispenser`의 Latest Release를 확인하고, 현재 버전보다
새로운 `IdentityDispenser.zip`을 자동으로 다운로드합니다. 해시 검증과 압축 해제가
끝난 뒤 앱을 종료하고 파일을 교체한 다음 재실행합니다.
인터넷 연결 실패, API 제한, 잘못된 패키지는 기존 버전 실행으로 넘어갑니다.

## 새 버전 배포 순서

1. `Assets/Resources/AutoUpdateSettings.json`의 `version`을 변경합니다.
   Unity 메뉴 `Tools > Identity Dispenser > Update Version Settings`로 파일을 찾을 수 있습니다.
   2026년 10월 15일은 `20261015`, 같은 날 수정판은 `20261015.1`입니다.
   현재 기본값 `20261001`은 기존 Release 날짜를 기준으로 잡은 값입니다.
2. Windows 빌드를 **전용 빈 폴더**에 만듭니다. 예: `Builds/Windows/IdentityDispenser.exe`.
   기존 배포와 실행 파일 이름을 유지합니다. 사용자 설정/로그가 들어 있는 사용 중인 폴더에 빌드하지 않습니다.
3. 빌드 완료 시 부모 폴더에 `IdentityDispenser.zip`이 자동 생성됩니다.
   예시 경로는 `Builds/IdentityDispenser.zip`입니다. 같은 경로의 이전 ZIP은 교체됩니다.
   ZIP에는 실행 파일, 데이터, 업데이트 스크립트, 파일 목록/버전 manifest가 포함됩니다.
4. GitHub에서 **새 Release**를 만들고 태그를 `20261015`로 지정합니다.
   제목은 `1015ver`로 자유롭게 지정할 수 있습니다. `1015ver` 태그도 지원하며
   이때 연도는 Release 게시 시각의 연도를 사용합니다. 연말/예약 배포 혼동을 피하려면
   연도가 포함된 태그를 사용하세요. 같은 날 수정판 태그는 `20261015.1`입니다.
5. 자동 생성된 `IdentityDispenser.zip`을 첨부하고 **Latest**로 게시합니다.
   Draft와 prerelease는 배포 대상이 아닙니다. 코드 push만으로 배포되지는 않습니다.
   GitHub에서 ZIP의 SHA-256 digest가 준비되어야 다운로드가 가능합니다.

Release 태그의 날짜/수정 번호와 빌드 설정 값이 일치해야 설치됩니다.
기존 `LimbusCompany`처럼 날짜가 없는 태그는 무시합니다.
자동 생성되는 Source code ZIP은 업데이트 파일이 아닙니다.

## 기존 사용자와 복구

- **기존 사용자는 이 기능이 포함된 ZIP을 한 번 직접 받아 압축을 풀어야 합니다.**
  그 이후부터 평소 실행 파일을 열면 자동 업데이트됩니다. 앱 실행 중에는 확인하지 않습니다.
- 설치 경로에 쓰기 권한이 필요합니다. 사용자 폴더 안에 압축을 풀어 사용하세요.
  PowerShell 실행을 차단하는 PC에서는 업데이트가 적용되지 않을 수 있습니다.
- `Application.persistentDataPath`와 PlayerPrefs의 사용자 설정은 교체하지 않습니다.
  설치 폴더에서는 manifest에 등록된 배포 파일만 관리합니다. 새 ZIP의 파일과 이름이 같은
  개인 파일은 교체되므로 개인 데이터는 설치 폴더 밖에 보관하세요.
- 이전 manifest에만 있는 파일은 삭제합니다. 파일 교체 실패 시 수정한 파일을 백업으로 복구합니다.
  실패 로그와 백업은 `%TEMP%/IdentityDispenserUpdate-<고유번호>`에 남습니다.
  실패 후 자동 재시도 루프를 방지하기 위해 기존 앱은 수동으로 다시 실행합니다.
- 정전/강제 종료 도중의 자동 복구는 보장하지 않습니다. 필요하면 최신 ZIP을 새 폴더에 풀어 실행합니다.

## 배포 전 확인

서로 다른 날짜 버전의 Windows 빌드 두 개로 실제 GitHub 업데이트/재실행을 확인하세요.
오프라인 실행, 잘못된 ZIP/해시, 쓰기 권한 없는 폴더, 중복 실행도 확인 대상입니다.
현재 코드 작업만으로 실제 공개 Release가 새로 게시되지는 않습니다.

개발 환경의 보조 검사:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tests/AutoUpdateChecks.ps1
```

프로젝트와 같은 Unity 버전 및 .NET SDK가 설치된 Windows에서 실행합니다.
실제 업데이트 도우미를 임시 앱에 적용하여 버전 파싱, 일반/폴더 포함 ZIP,
잘못된 해시/버전/압축 경로 거부, 잠긴 파일 교체 실패 시 복구를 확인합니다.
임시 파일은 Git에서 제외되는 `Temp/AutoUpdateChecks`에 생성됩니다.

## 대용량 업데이트 준비

다운로드한 ZIP의 SHA-256은 설치 도우미가 압축 해제 전에 한 번 검증합니다.
준비 화면에는 검증·압축 해제·설치 파일 확인 단계가 표시됩니다.
준비에 3분 이상 걸려도 작업이 진행 중이면 계속 기다리며,
진행 기록이 15분 동안 바뀌지 않을 때 준비 시간 초과로 처리합니다.
실패 시 임시 폴더의 `error.txt`(도우미) 또는 `startup-error.txt`(앱)로 원인을 확인할 수 있습니다.
수정된 동작은 새 Windows 빌드를 배포한 뒤 적용됩니다.
