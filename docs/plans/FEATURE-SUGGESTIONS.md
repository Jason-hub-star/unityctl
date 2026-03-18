# Unity 공식 API 기반 기능 확장 제안

> 기준: Unity 6 (6000.x) 공식 ScriptReference + unityctl 현재 54개 명령 대비 누락 분석
> 작성일: 2026-03-18
> 구현 가능성(%): 기존 아키텍처(IPC + Plugin Handler + CLI Command) 재사용 난이도 기준

---

## Category A: Lighting & Baking (조명)

- [ ] `lighting bake` — Lightmapping.BakeAsync() 호출, 비동기 베이킹 시작 (85%)
- [ ] `lighting cancel` — Lightmapping.Cancel() 진행 중인 베이킹 취소 (95%)
- [ ] `lighting clear` — Lightmapping.Clear() 라이트맵 데이터 초기화 (95%)
- [ ] `lighting get-settings` — LightingSettings 속성 조회 (albedoBoost, lightmapResolution 등) (80%)
- [ ] `lighting set-settings` — LightingSettings 속성 설정 (80%)

## Category B: NavMesh (내비게이션)

- [ ] `navmesh bake` — NavMeshBuilder.BuildNavMesh() 에디터 베이킹 (70%)
- [ ] `navmesh clear` — NavMeshBuilder.ClearAllNavMeshes() 초기화 (85%)
- [ ] `navmesh get-settings` — NavMeshBuildSettings 조회 (agentRadius, agentHeight 등) (75%)

## Category C: Tags & Layers (태그/레이어 관리)

- [ ] `tag list` — InternalEditorUtility.tags 전체 태그 목록 (95%)
- [ ] `tag add` — InternalEditorUtility.AddTag() 커스텀 태그 추가 (90%)
- [ ] `layer list` — InternalEditorUtility.layers 전체 레이어 목록 (95%)
- [ ] `layer set` — SerializedObject("TagManager") 레이어 이름 설정 (80%)
- [ ] `gameobject set-tag` — GameObject.tag 설정 (기존 set-property 확장) (90%)
- [ ] `gameobject set-layer` — GameObject.layer 설정 (90%)

## Category D: Physics Settings (물리 설정)

- [ ] `physics get-collision-matrix` — Physics.GetIgnoreLayerCollision 매트릭스 조회 (80%)
- [ ] `physics set-collision-matrix` — Physics.IgnoreLayerCollision 레이어 충돌 설정 (80%)
- [ ] `physics get-settings` — Physics.gravity, Physics.defaultContactOffset 등 조회 (85%)
- [ ] `physics set-settings` — 물리 글로벌 설정 변경 (85%)

## Category E: Build Profiles & Platform (빌드 프로필)

- [ ] `build-profile list` — BuildProfile 에셋 목록 조회 (75%)
- [ ] `build-profile get-active` — BuildProfile.GetActiveBuildProfile() (80%)
- [ ] `build-profile set-active` — BuildProfile.SetActiveBuildProfile() 프로필 전환 (75%)
- [ ] `build-target switch` — EditorUserBuildSettings.SwitchActiveBuildTarget() 플랫폼 전환 (70%)
- [ ] `build-settings get-scenes` — EditorBuildSettings.scenes 빌드 씬 목록 조회 (90%)
- [ ] `build-settings set-scenes` — EditorBuildSettings.scenes 빌드 씬 목록 설정 (80%)

## Category F: Asset Search & Query (에셋 검색)

- [ ] `asset find` — AssetDatabase.FindAssets(filter) + t:/l: 필터 지원 (90%)
- [ ] `asset get-info` — AssetDatabase.GetAssetPath, GetMainAssetTypeAtPath 메타 조회 (90%)
- [ ] `asset get-dependencies` — AssetDatabase.GetDependencies() 의존성 그래프 (85%)
- [ ] `asset get-labels` — AssetDatabase.GetLabels() 라벨 조회 (90%)
- [ ] `asset set-labels` — AssetDatabase.SetLabels() 라벨 설정 (90%)

## Category G: Addressables / AssetBundle (콘텐츠 빌드)

- [ ] `addressables build` — AddressableAssetSettings.BuildPlayerContent() (55%)
- [ ] `addressables clean` — AddressableAssetSettings.CleanPlayerContent() (55%)
- [ ] `assetbundle build` — BuildPipeline.BuildAssetBundles() (60%)

## Category H: Terrain (터레인)

- [ ] `terrain get-heightmap` — TerrainData.GetHeights() 높이맵 조회 (60%)
- [ ] `terrain set-height` — TerrainData.SetHeights() 높이맵 수정 (55%)
- [ ] `terrain add-tree` — TerrainData.treeInstances 트리 배치 (50%)
- [ ] `terrain paint-detail` — TerrainData.SetDetailLayer() 디테일 페인트 (45%)

## Category I: Script & Code Generation (스크립트)

- [ ] `script create` — ProjectWindowUtil.CreateScriptAssetFromTemplateFile() C# 스크립트 생성 (85%)
- [ ] `script list` — MonoScript 기반 프로젝트 내 스크립트 목록 (80%)
- [ ] `define-symbols get` — PlayerSettings.GetScriptingDefineSymbolsForGroup() (90%)
- [ ] `define-symbols set` — PlayerSettings.SetScriptingDefineSymbolsForGroup() (85%)

## Category J: Editor Utility (에디터 유틸리티)

- [ ] `screenshot capture` — ScreenCapture.CaptureScreenshot() 게임뷰 캡처 (80%)
- [ ] `editor pause` — EditorApplication.isPaused 에디터 일시정지 (90%)
- [ ] `editor focus-gameview` — EditorApplication.ExecuteMenuItem("Window/General/Game") (75%)
- [ ] `editor focus-sceneview` — SceneView.lastActiveSceneView.Focus() (75%)
- [ ] `editor refresh` — EditorApplication.UnlockReloadAssemblies + DomainReload 트리거 (65%)
- [ ] `console clear` — LogEntries.Clear() 콘솔 로그 클리어 (90%)
- [ ] `console get-count` — LogEntries.GetCountsByType() 로그/경고/에러 카운트 (85%)

## Category K: Timeline & Cinemachine (시네마틱)

- [ ] `timeline create` — TimelineAsset 생성 + PlayableDirector 바인딩 (50%)
- [ ] `timeline add-track` — TimelineAsset.CreateTrack<T>() 트랙 추가 (45%)
- [ ] `cinemachine create-camera` — CinemachineVirtualCamera 생성 (50%)

## Category L: Sprite & 2D (2D 전용)

- [ ] `sprite-atlas create` — SpriteAtlas 에셋 생성 (65%)
- [ ] `sprite-atlas add` — SpriteAtlas.Add() 스프라이트 추가 (60%)
- [ ] `sprite set-import` — TextureImporter 2D 스프라이트 임포트 설정 (70%)

## Category M: Audio (오디오)

- [ ] `audio-mixer create` — AudioMixer 에셋 생성 (60%)
- [ ] `audio-mixer get-volume` — AudioMixer.GetFloat() 파라미터 조회 (65%)
- [ ] `audio-mixer set-volume` — AudioMixer.SetFloat() 파라미터 설정 (65%)

---

## 우선순위 요약 (구현 가능성 순)

### Tier 1 — 높은 가능성 (85-95%) — 기존 패턴 그대로 적용
| 명령 | 가능성 |
|------|--------|
| tag list / layer list | 95% |
| lighting cancel / clear | 95% |
| tag add | 90% |
| gameobject set-tag / set-layer | 90% |
| asset find / get-info / get-labels / set-labels | 90% |
| build-settings get-scenes | 90% |
| console clear / editor pause | 90% |
| define-symbols get | 90% |
| lighting bake | 85% |
| asset get-dependencies | 85% |
| physics get/set-settings | 85% |
| console get-count | 85% |
| script create | 85% |
| define-symbols set | 85% |
| navmesh clear | 85% |

### Tier 2 — 중간 가능성 (70-84%) — 약간의 추가 설계 필요
| 명령 | 가능성 |
|------|--------|
| lighting get/set-settings | 80% |
| layer set | 80% |
| physics collision-matrix | 80% |
| build-profile get/set-active | 75-80% |
| build-settings set-scenes | 80% |
| script list | 80% |
| screenshot capture | 80% |
| navmesh get-settings | 75% |
| editor focus-gameview/sceneview | 75% |
| build-profile list | 75% |
| build-target switch | 70% |
| navmesh bake | 70% |
| sprite set-import | 70% |

### Tier 3 — 도전적 (45-69%) — 외부 패키지 의존 또는 복잡한 데이터
| 명령 | 가능성 |
|------|--------|
| sprite-atlas create/add | 60-65% |
| audio-mixer | 60-65% |
| assetbundle build | 60% |
| terrain get-heightmap | 60% |
| addressables build/clean | 55% |
| terrain set-height | 55% |
| timeline create | 50% |
| terrain add-tree | 50% |
| cinemachine create-camera | 50% |
| timeline add-track | 45% |
| terrain paint-detail | 45% |

---

## 참고 자료 (Unity 공식 문서)

- [Lightmapping API](https://docs.unity3d.com/ScriptReference/Lightmapping.html)
- [NavMeshBuilder API](https://docs.unity3d.com/ScriptReference/AI.NavMeshBuilder.html)
- [Tags & Layers](https://docs.unity3d.com/6000.3/Documentation/Manual/class-TagManager.html)
- [Physics API](https://docs.unity3d.com/ScriptReference/Physics.html)
- [BuildProfile API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Profile.BuildProfile.SetActiveBuildProfile.html)
- [AssetDatabase.FindAssets](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.FindAssets.html)
- [Addressables Build Scripting](https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/build-intro.html)
- [Terrain API](https://docs.unity3d.com/ScriptReference/Terrain.html)
- [BuildPipeline API](https://docs.unity3d.com/ScriptReference/BuildPipeline.html)
- [VisualEffect API](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html)
- [EditorApplication API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorApplication.html)
