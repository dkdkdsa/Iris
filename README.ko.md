[English](README.md) | **한국어**

# Iris

Iris는 C#으로 개발 중인 2D 전용 게임 엔진입니다.
C# 런타임과 GUI 에디터 등이 포함되어 있습니다.

개발 진행 중인 프로젝트로, API나 엔진 코드 등이 예고 없이 바뀔 수 있습니다.

---

## 미리 보기

![Iris 에디터](docs/images/Editor.png)

---

## 지원 플랫폼

| 구분 | 지원 |
| --- | --- |
| 에디터 | Windows 10 / 11 (x64) |
| 게임 런타임 | Windows 10 / 11 (x64) |

- 스크립트 편집은 Visual Studio가 설치되어 있으면 연동되고, 없으면 OS 기본 프로그램으로 열립니다.
- 차후 Linux, iOS, Android 등 크로스 플랫폼 지원을 추가할 예정입니다.

---

## 시작하기

### 1. .NET 10 SDK 설치

[.NET 10 SDK 다운로드](https://dotnet.microsoft.com/download/dotnet/10.0)

### 2. 에디터 준비

**릴리즈 다운로드** — [Releases](https://github.com/dkdkdsa/Iris/releases)에서 최신 버전을 받아 압축을 풀고 `IrisEditor.exe`를 실행합니다.

**소스에서 빌드**

```bash
git clone https://github.com/dkdkdsa/Iris.git
cd Iris
dotnet run --project IrisEditor
```

### 3. 프로젝트 만들기

에디터에서 **File -> New Project**로 빈 폴더를 지정하면 게임 프로젝트가 생성됩니다.
씬을 편집하고 **F5**를 눌러 현재 씬을 테스트해 볼 수 있습니다.

### 4. 프로젝트 빌드

**File -> Build**를 눌러 현재 프로젝트를 빌드할 수 있습니다.

---

## 샘플

![Iris Pong](docs/images/ExampleGame.png)

| 샘플 | 설명 | 링크 |
| --- | --- | --- |
| Pong | AI와 대전하는 핑퐁 | [IrisPong](https://github.com/dkdkdsa/IrisPong) |
| _(준비 중)_ | — | — |

---

## 라이선스

MIT — [LICENSE](LICENSE)

서드파티 구성요소와 그 라이선스 전문은 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)에 있습니다.

Iris로 빌드한 게임에는 `SDL2.dll`, `cimgui.dll`, `stbi.dll` 등 네이티브 바이너리가 함께 복사됩니다. **게임을 배포할 때 이 고지 파일도 함께 배포해야 합니다.**
