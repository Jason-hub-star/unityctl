# unityctl 용어집

이 문서는 프로젝트 전반에서 쓰이는 주요 용어를 설명합니다.
항목은 알파벳 순으로 정렬되어 있습니다.

## A

`Agent Layer`: AI 에이전트가 하위 unityctl 명령들을 조합해 더 큰 작업 흐름을 만드는 최상위 계층입니다.

`AppLocker`: 특정 실행 파일이나 어셈블리의 실행·로딩을 막을 수 있는 Windows 정책 계층입니다.

`Accepted`: 비동기 작업이 시작되었음을 나타내는 StatusCode (104). CLI는 `test-result`로 폴링하여 완료를 확인합니다.

`async command`: 즉시 끝나지 않고, 나중에 콜백으로 결과를 돌려주는 Unity 명령입니다.

`AsyncCommandRunner`: CLI에서 Accepted 응답 후 `test-result`를 주기적으로 폴링하여 비동기 명령 완료를 대기하는 실행기입니다. delegate 주입으로 테스트 가능합니다.

`AsyncOperationRegistry`: Plugin 내에서 비동기 작업 상태를 관리하는 저장소입니다. single-flight guard, age-check, TTL prune을 제공합니다.

## B

`backpressure`: 이벤트가 너무 빨리 들어오고 소비자가 느릴 때 시스템이 버티는 방식입니다.

`BatchTransport`: Unity를 batchmode로 새로 띄우고, 요청 파일과 응답 파일을 통해 명령을 처리하는 전송 방식입니다.

`batchmode`: Unity를 비대화형으로 실행하는 모드입니다. 자동화, CI, 커맨드라인 작업에 주로 사용합니다.

`black-box CLI test`: 내부 클래스를 직접 호출하지 않고, 실제 빌드된 `unityctl` 실행 파일을 프로세스로 띄워 검증하는 테스트입니다.

## C

`CLI`: 명령줄 인터페이스입니다. 이 프로젝트에서는 터미널에서 직접 실행하는 `unityctl` 프로그램을 뜻합니다.

`CommandExecutor`: 어떤 transport를 쓸지 고르고, 재시도를 적용하고, 최종 응답을 반환하는 핵심 오케스트레이터입니다.

`Command Layer`: `status`, `check`, `test`, `build` 같은 결정적 명령을 담당하는 계층입니다.

`CommandRegistry`: Unity 쪽에서 명령 이름을 실제 핸들러에 연결해 주는 디스패처입니다.

`CommandRequest`: CLI가 Unity에 보내는 요청 DTO입니다. 명령 이름, 파라미터, 요청 ID를 담습니다.

`CommandResponse`: Unity가 CLI에 돌려주는 응답 DTO입니다. 성공 여부, 상태 코드, 메시지, 데이터, 에러를 담습니다.

`compile-only retry`: Unity가 첫 batch 실행에서 실제 명령 대신 스크립트 컴파일만 하고 끝났을 때 자동으로 한 번 더 실행하는 복구 패턴입니다.

`CompileWatcher`: `CompilationPipeline.compilationStarted/Finished` 이벤트를 감시하여 컴파일 상태를 실시간 스트리밍하는 Watch Mode 컴포넌트입니다.

`control plane`: 상태 조회, 명령 실행, 세션 유지, 기록을 통합 제어하는 계층이라는 뜻입니다. 이 프로젝트의 장기 정체성을 설명하는 표현입니다.

## D

`daemon`: 명령이 끝나도 종료되지 않고 백그라운드에서 계속 살아 있는 프로세스입니다. 세션, IPC, 스트리밍 기능의 기반이 됩니다.

`deterministic`: 같은 입력과 같은 환경에서 같은 결과를 재현 가능하게 돌려주는 성질입니다.

`Domain Reload`: Unity가 스크립트를 다시 로드하는 과정입니다. 코드 변경 후 발생하며, 명령 실행이나 IPC 콜백을 끊을 수 있습니다.

`dry-run`: 실제 작업은 하지 않고, 어떤 일이 일어날지만 미리 보여주는 실행 모드입니다.

## E

`EventEnvelope`: 콘솔 로그, hierarchy 변경, selection 변경 같은 스트리밍 이벤트를 감싸는 공통 DTO입니다.

## F

`Flight Recorder`: `unityctl`이 어떤 명령을 언제 어떤 결과로 실행했는지 append-only 로그로 남기는 기능입니다.

## G

`Ghost Mode`: 실제 적용 없이 무엇이 바뀔지 미리 보여주는 예정 기능입니다.

## I

`IAsyncUnityctlCommand`: 비동기적으로 끝나는 Unity 명령용 인터페이스입니다. 나중에 콜백으로 결과를 돌려줍니다.

`IpcServer`: Unity Editor 안에서 CLI 연결을 받아들이고 요청을 라우팅하는 서버입니다.

`IpcTransport`: 이미 열려 있는 Unity Editor와 IPC로 통신하는 전송 방식입니다.

`IPC`: 프로세스 간 통신입니다. CLI와 Unity Editor가 서로 다른 프로세스로 실행될 때 메시지를 주고받게 해줍니다.

`ITransport`: 명령 전달과 이벤트 구독을 추상화한 공통 인터페이스입니다.

## J

`JSON workflow`: `unityctl exec --workflow workflow.json`으로 여러 커맨드를 순차 실행하는 방식입니다. 커스텀 DSL 대신 JSON 파일로 워크플로우를 정의하여 구현 복잡도를 줄입니다.

`JsonContext`: `System.Text.Json`의 소스 생성 직렬화 컨텍스트입니다.

`JsonConvert`: Newtonsoft.Json에서 사용하는 주요 JSON 직렬화 API입니다.

`JsonObject`: `Dictionary<string, object>` 대신 구조를 안정적으로 보존하기 위해 쓰는 JSON 객체 노드 타입입니다.

`JsonUtility`: Unity 내장 JSON 직렬화 도구입니다. 가볍지만 동적 구조 처리에는 제약이 큽니다.

`JObject`: Newtonsoft.Json에서 사용하는 JSON 객체 타입입니다. Unity 플러그인 쪽에서 유연한 payload 처리에 사용됩니다.

## M

`MCP`: Model Context Protocol입니다. 에이전트-도구 통합에 강한 프로토콜이며, 이 프로젝트는 MCP의 상위 호환을 목표로 합니다. 필요시 MCP C# SDK의 `[McpServerToolType]` / `[McpServerTool]`로 ~100줄 수준의 브릿지가 가능합니다.

`MCP bridge`: unityctl 커맨드를 MCP 서버로 래핑하는 선택적 호환 레이어입니다. MCP C# SDK v1.0을 활용하며 Phase 5 이후 제공 예정입니다.

`Named Pipe`: Windows에서 사용하는 IPC 방식입니다.

## N

`NDJSON`: 줄바꿈 단위 JSON 형식입니다. 한 줄이 하나의 JSON 객체라 append-only 로그 작성과 `tail`에 적합합니다.

`NormalizeProjectPath`: CLI와 Plugin이 같은 프로젝트를 같은 세션/파이프 이름으로 인식하도록 경로를 표준화하는 규칙입니다.

## P

`payload`: 요청, 응답, 이벤트 안에 실려 있는 실제 구조화 데이터입니다.

`Phase 2A`: 직렬화 수정, Core 추출, transport 추상화, 테스트 안정화를 담당하는 기반 정비 단계입니다. 완료 상태.

`payload typed accessor`: `GetParam<T>()`와 `GetObjectParam()`을 통해 파라미터를 타입 안전하게 꺼내는 패턴입니다. Shared(JsonObject)와 Plugin(JObject) 양쪽에 동일한 API로 제공됩니다.

`Phase 2B`: 열린 Editor와 빠르게 통신하기 위한 IPC 구현 단계입니다.

`Phase 2C`: 비동기 명령 처리 단계입니다. Polling 모델(Accepted → test-result), single-flight guard, leaf-only 집계, IErrorCallbacks, AsyncCommandRunner를 구현했습니다. 완료 상태.

`polling model`: CLI가 초기 Accepted 응답 후 주기적으로 `test-result`를 조회하여 비동기 명령 완료를 확인하는 패턴입니다.

`Platform abstraction`: 운영체제별 차이를 인터페이스 뒤로 숨겨 상위 로직을 공통으로 유지하려는 설계 방식입니다.

`Plugin`: Unity 프로젝트 안에 들어가는 UPM 패키지입니다. 실제 Unity API 호출과 명령 수행을 담당합니다.

`ProjectLocked`: 현재 transport 관점에서 대상 Unity 프로젝트를 안전하게 열거나 실행할 수 없는 상태를 뜻하는 상태 코드입니다.

## R

`response file`: Unity batchmode가 결과를 JSON 파일로 쓰고, CLI가 그 파일을 읽는 방식의 응답 채널입니다.

`RetryPolicy`: 상태 코드에 따라 재시도할지, 지연할지, 즉시 실패할지를 결정하는 정책 컴포넌트입니다.

`ring buffer`: 고정 크기의 버퍼입니다. 가득 차면 오래된 이벤트를 버리고 최신 이벤트를 유지합니다.

## S

`Scene Diff`: 씬 스냅샷 두 개를 비교해서 추가·삭제·변경된 구조를 보여주는 예정 기능입니다. `SerializedProperty.DataEquals()` API를 활용하여 YAML 파싱 없이 의미적 비교를 수행합니다.

`SerializedProperty.DataEquals()`: Unity 내장 API로, 두 `SerializedProperty` 값이 같은지 비교합니다. Scene Diff에서 YAML 파싱 대신 사용하여 프리팹 오버라이드도 자동 처리합니다.

`Session Layer`: 열린 Unity Editor와 장기 연결을 유지하면서 빠른 상호작용을 담당하는 계층입니다.

`SessionManager`: 세션 생성, 추적, 종료를 담당하는 Core 컴포넌트입니다.

`SessionStore`: 세션 메타데이터를 파일로 저장하는 저장소입니다.

`Shared`: CLI와 Unity 플러그인이 함께 쓰는 프로토콜/상수 라이브러리입니다.

`stale lockfile`: 실제 Unity는 꺼졌지만 잠금 파일만 남아 있어서 프로젝트가 바쁜 것처럼 보이는 상태입니다.

`StatusCode`: `Ready`, `Compiling`, `ProjectLocked`, `UnknownError` 같은 결과 상태를 문자열 파싱 없이 다루기 위한 enum입니다.

`streaming`: 요청 하나에 응답 하나를 받는 대신, 연결을 유지하며 이벤트를 계속 전달받는 방식입니다.

## T

`tmux for Unity`: 장기 세션, 재접속, 지속 작업 흐름을 강조하기 위해 비전 문서에서 쓰는 비유 표현입니다.

`tool discovery`: `unityctl tools --json`으로 AI 에이전트가 사용 가능한 커맨드를 동적으로 발견하는 기능입니다. MCP의 `tools/list`를 대체합니다.

`transport`: 명령을 실제로 전달하는 방식입니다. 예를 들어 batchmode 또는 IPC가 transport가 됩니다.

`TransportCapability`: 각 transport가 지원하는 기능을 나타내는 플래그입니다. 예를 들어 스트리밍 가능 여부, 저지연 여부 등이 있습니다.

## U

`UDS`: Unix Domain Socket입니다. macOS와 Linux에서 사용할 IPC 방식입니다.

`Unityctl.Cli`: 터미널에서 사용자가 직접 실행하는 CLI 프로젝트입니다.

`Unityctl.Core`: 실행, transport, 세션, 로깅 같은 핵심 비즈니스 로직을 담는 Core 라이브러리입니다. Phase 2A에서 CLI의 Platform/Infrastructure 코드를 추출하여 생성되었습니다.

`Unityctl.Plugin`: Unity Editor 안에서 돌아가는 UPM 브릿지 패키지입니다.

`Unityctl.Shared`: 프로토콜 계약, 공통 모델, 상수를 담는 공유 라이브러리입니다.

`Unity Control Plane`: 사람, CI, AI 에이전트가 공통 방식으로 Unity를 제어하는 결정적 실행 계층이라는 장기 비전 표현입니다.

`UnityEditorDiscovery`: 설치된 Unity Editor를 찾고, 프로젝트에 맞는 에디터 버전을 매칭하는 컴포넌트입니다.

`UnityProcessDetector`: 현재 실행 중인 Unity 프로세스를 찾아 어떤 프로젝트를 열고 있는지 추적하는 컴포넌트입니다. Phase 2A에서 stub으로 생성, Phase 2B에서 WMI/ps 기반 구현 예정입니다.

`UnityctlBatchEntry`: `-executeMethod`로 batchmode에서 Unity가 진입하는 메서드입니다.

`UPM`: Unity Package Manager입니다. Unity의 공식 패키지 시스템입니다.

## V

`VYaml`: YamlDotNet 대비 6배 빠른 고성능 YAML 파서입니다. Unity의 `stripped` 태그를 네이티브 지원하며, Scene Diff의 외부 파싱 대안으로 확보되어 있습니다.

## W

`Watch Mode`: 콘솔, hierarchy, selection 같은 Editor 이벤트를 실시간 스트리밍으로 보는 예정 기능입니다.

`wire format`: 요청, 응답, 이벤트가 실제로 전송되거나 저장될 때의 정확한 데이터 형식입니다.

## X

`xUnit`: 현재 저장소의 .NET 테스트 프로젝트에서 사용하는 테스트 프레임워크입니다.
