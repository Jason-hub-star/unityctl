[P1] batch fallback에서는 아직 --no-wait semantics를 만족시킬 수 없습니다. 이건 현재 transport 구조에서 바로 나오는 추론입니다. BatchTransport.cs#L82 에서 CLI는 Unity 프로세스 종료까지 기다리고, revised plan의 UnityctlBatchEntry도 Accepted 후 내부 대기를 하므로, Unity가 떠 있지 않은 경우 --no-wait나 PlayMode 강제 --no-wait가 즉시 반환되지 않습니다. 플랜에 --no-wait는 IPC 전용이라고 못 박거나, batch + --no-wait를 명시적으로 unsupported 처리하는 쪽이 안전합니다.

[P1] “run이 시작조차 못한 경우” 완료 경로가 아직 비어 있습니다. revised plan은 ICallbacks 중심인데, Unity 공식 문서는 IErrorCallbacks.OnError가 컴파일 오류나 IPrebuildSetup 예외처럼 RunFinished 전에 실패한 경우 호출된다고 설명합니다. 이 경로를 안 넣으면 registry가 Running에 남고 CLI는 timeout까지 폴링할 수 있습니다. 출처: How to get test results

[P2] non-JSON 출력에서는 Accepted가 최종 성공처럼 보입니다. ConsoleOutput.cs#L7 는 Success=true면 전부 OK로 출력합니다. 그래서 test --no-wait가 완료 성공과 시각적으로 구분되지 않습니다. Accepted를 유지할 거면 ACCEPTED [104] 같은 출력 분기가 하나 필요합니다.

[P2] Running TTL 30분은 single-flight lockout을 너무 길게 만들 수 있습니다. timeout, reload, start-error가 한 번 나면 다음 test가 prune 전까지 오래 막힐 수 있습니다. 특히 CLI 기본 timeout이 300초라서 gap이 큽니다. HasRunning()에서 즉시 age-check를 하거나, running TTL을 CLI timeout + buffer 수준으로 줄이는 편이 덜 거칠습니다.

[P3] revised text의 한 전제는 다시 고치는 게 좋습니다: TestRunnerApi.Execute는 void가 아니라 GUID를 반환합니다. 콜백 혼선 문제를 해결해주진 않지만, 진단 로그나 향후 cancel 지원에는 유용합니다. 출처: Execute

남은 큰 뼈대는 꽤 좋아졌습니다. 특히 single-flight, leaf-only, idempotent polling, catalog 비노출 결정은 방향이 맞습니다. 위 5개만 플랜에 한 번 더 반영하면 구현 들어가도 될 수준으로 보입니다.

부가로 두 가지만 더 보면 좋겠습니다. EditMode는 reload가 없다는 문장은 조금 약하게 쓰는 편이 안전하고, AsyncCommandRunner 테스트는 레이어 기준상 tests/Unityctl.Cli.Tests가 더 자연스럽습니다.