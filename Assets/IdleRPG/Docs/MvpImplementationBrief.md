# MVP Implementation Brief

작성일: 2026-08-20
최종 갱신일: 2026-09-02

## 요약

현재 프로젝트는 Week 1 Vertical Slice 기준으로 Unity 에디터에서 바로 Play 테스트 가능한 MVP 상태까지 구성되어 있다.
각 스크립트별 상세 역할은 `ScriptRoleGuide.md`를 기준 문서로 사용한다.

구현된 핵심 흐름은 다음과 같다.

- `SampleScene`과 `Week1VerticalSlice`에 MVP 전용 컨트롤러, Actor, HUD, EventSystem 배치
- 에디터에는 `Player Start Point` 위치 마커만 배치하고, 실제 Hero Actor는 Play 시작 시 런타임 생성
- Monster Actor는 기본 배치에서 제외하고 `MonsterSpawner`를 통해 런타임 스폰
- 자동 타겟 탐색, 접근, 기본 공격, HP 감소, 사망 이벤트, 보상, 스테이지 진행 구현
- `DefaultTargetSelector`와 `CombatRangePolicy`로 타겟 선정/사거리 판정 분리
- `ActorStateMachine`과 `StatModifier`로 상태 전환과 버프형 스탯 확장 기반 추가
- `CombatLoopMode`와 `ICombatLoop`로 실시간/턴제 전투 루프가 동시에 실행되지 않도록 배타화
- `IDamageCalculator`, `ISkillExecutor`, `ISkillEffect`로 전투 계산/스킬 실행 인터페이스 분리
- `SkillDefinition`, `SkillRuntime`, `SkillLoadout`으로 스킬 ID, 쿨다운, 사거리, 우선순위, 4 Slot Loadout 구조 추가
- `SkillExecutor`, `DamageSkillEffect`, `BuffSkillEffect`로 사용 가능 조건, 타겟 검증, 쿨다운, 피해/버프 효과 실행 흐름 구현
- 실시간/턴제 자동전투가 기본 공격 전에 사용 가능한 스킬을 우선 실행하도록 연결
- 필드 조우에서 전투 진입을 요청할 수 있는 `FieldEncounterController`와 `StageSceneFlowController` 추가
- 실시간 전투와 분리된 턴제 자동전투 초안 `TurnBasedAutoBattleController` 추가
- Hero 사망 시 현재 Stage 재시작 UI 표시 및 재시작 시 위치/HP/상태 회복
- 디자이너가 Inspector에서 플레이어/몬스터/스테이지/타겟팅/전투 방식/배치/색상/문구/타이밍을 조절할 수 있도록 설정 그룹 추가
- 화면 상단 HUD에 Stage, Gold, EXP, Player/Enemy HP, 전투 로그 표시
- 씬이 열리거나 Play 진입 직전에 레이아웃과 슬롯을 자동 보정하는 에디터 유틸리티 추가
- Unity batchmode 스모크 테스트로 씬 슬롯과 런타임 초기화 경로 검증

## 플레이 방법

1. Unity에서 `Assets/Scenes/SampleScene.unity`를 연다.
2. 외부 변경 감지 팝업이 뜨면 `Reload`를 선택한다.
3. Play 버튼을 누른다.
4. 화면에 `Training Hero`와 첫 번째 몬스터가 배치되고 자동 전투가 시작된다.

추가 확인이 필요하면 Unity 메뉴에서 `Idle RPG > Run MVP Scene Smoke Test`를 실행한다.

Actor 프리팹 뼈대가 필요하면 Unity 메뉴에서 `Idle RPG > Prefabs > Actor > Create Actor Prefab`을 연다.
Monster 프리팹 뼈대가 필요하면 `Idle RPG > Prefabs > Monster > Create Monster Prefab`을 연다.
기본값으로 바로 생성하려면 각각 `Create Default Actor Prefab`, `Create Default Monster Prefab`을 실행한다.
타일맵 배치를 조절하려면 `Idle RPG > Maps > Tile Map Editor`를 연다.

## 씬 구성

두 씬 모두 동일한 MVP 레이아웃을 저장해 두었다.

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/Week1VerticalSlice.unity`

씬에 저장된 주요 오브젝트는 다음과 같다.

- `MVP Scene Controller`: 전체 MVP 씬 구성과 런타임 시작점
- `World`: 전투 월드 루트
- `Combat Tile Map`: 사각형 전투 타일맵 루트
- `Tiles`: 사각형 타일 시각 요소 묶음
- `Player Start Point`: Hero 시작 위치를 나타내는 빈 Transform 기준점. 렌더링/전투 컴포넌트는 없음
- `Monster Spawn Point`: 몬스터 런타임 생성 위치
- `MVP HUD Canvas`: Screen Space Overlay HUD
- `Status Panel`: Stage, 자원, HP, 로그 표시 패널
- `Restart Panel`: Hero 사망 시 현재 Stage를 처음부터 다시 시작하는 UI
- `EventSystem`: UI 입력 이벤트 시스템

`MvpSceneController`에는 디자이너가 조절하는 설정과 자동 할당되는 씬 참조가 나뉘어 있다.

- `Game Content`: Player, Skill, Monster, Stage 데이터와 Player/Monster Skill Loadout
- `Designer Settings`: Camera, World Layout, Actor View, Combat Loop, Monster Spawn, Scene Flow, Field Encounter, Turn Combat, HUD, Restart Popup, Stage Runtime
- `PlayerStartPoint`
- `MonsterSpawnPoint`
- `HudCanvas`
- `StageText`
- `ResourceText`
- `PlayerText`
- `EnemyText`
- `LogText`
- `PlayerHpFill`
- `EnemyHpFill`
- `RestartPanel`
- `RestartButton`

## 코드 구조

### Domain

경로: `Assets/IdleRPG/Scripts/Domain`

Unity 의존성이 없는 순수 게임 규칙 계층이다.

- `ActorModel`: Actor의 상태, HP, 사망 이벤트 관리
- `SkillDefinition`, `SkillRuntime`, `SkillLoadout`: 스킬 정적 데이터, 런타임 쿨다운, 4칸 장착 슬롯
- `DamageSkillEffect`, `BuffSkillEffect`, `SkillExecutionResult`: 스킬 효과 실행과 결과 데이터
- `StatBlock`: HP, 공격력, 방어력, 사거리, 이동속도, 공격간격, 치명타 관련 수치
- `StatModifier`: 버프/디버프용 additive/multiplier 스탯 변형 값
- `ActorStateMachine`: Dead 상태 잠금과 Restore 시 Idle 복귀를 담당하는 상태 전환 단위
- `GlobalEnums`: 여러 객체가 공유하는 전역 enum 관리
- `ActorTeam`: Player/Monster 팀 구분
- `ActorState`: Search, Move, Attack, Dead 등 전투 상태
- `TargetSelectionMode`: Nearest, LowestHp, HighestAttack 타겟 선정 기준
- `CombatLoopMode`: Realtime/TurnBased 전투 루프 선택
- `StageFlowMode`: Field/Battle 흐름 구분
- `EncounterTriggerMode`: Manual/Distance 조우 트리거 구분
- `CombatMath`: 기본 공격 피해량 계산
- `DamageResult`: 피해량, 치명타 여부 등 계산 결과
- `PlayerDefinition`, `MonsterDefinition`, `StageDefinition`: 런타임 콘텐츠 정의
- `RuntimeContentDatabase`: Player, Skill, Monster, Stage 정의 데이터 조회

### Runtime

경로: `Assets/IdleRPG/Scripts/Runtime`

Unity 씬에서 실제로 동작하는 계층이다.

- `MvpSceneController`: 씬 오브젝트, HUD, Actor 배치 및 Play 시 런타임 초기화
- `MvpGameContentSettings`: Inspector에서 조절 가능한 Player, Skill, Monster, Stage 콘텐츠 설정
- `MvpSceneDesignerSettings`: Inspector에서 조절 가능한 카메라, 타일맵, Actor 표시, 타겟팅, 필드 조우, 전투 방식, HUD, 재시작 팝업, 스테이지 런타임 설정
- `GeneratedSpriteFactory`: 임시 유닛 스프라이트와 사각형 타일 스프라이트 생성
- `DemoContentFactory`: 기본 Week 1 설정을 `RuntimeContentDatabase`로 변환하는 호환용 팩토리
- `ActorFactory`: GameObject에 전투 Actor 컴포넌트 묶음, 월드 HP 바, Display Name 라벨 구성
- `CombatActor`: ActorModel과 SpriteRenderer를 연결하고 피격/사망 이벤트 발행
- `TileMapLayout`: 사각형 타일맵 생성, 셀/월드 좌표 변환, Sprite Palette 기반 타일 렌더링, 막힌 칸 판정, 경로 첫 칸 계산, y축 기준 정렬 순서 계산
- `AutoCombatController`: 타겟 셀렉터 결과를 받아 스킬 우선 사용, 월드 좌표 기반 이동, 기본 공격 수행. `UseTileMovement`를 켜면 기존 타일 이동 경로도 사용할 수 있음
- `BattleContext`: 살아 있는 Actor 등록, 타일맵 참조, 타겟팅 설정, 타겟 선정 진입점 관리
- `DefaultTargetSelector`: 설정된 기준에 맞춰 유효한 적 Actor 선택
- `CombatRangePolicy`: 월드 거리 기반 사거리/접근 위치 계산
- `ICombatLoop`: Realtime/TurnBased 전투 루프 활성 상태를 공통으로 제어하는 인터페이스
- `TurnBasedAutoBattleController`: 턴 딜레이에 맞춰 살아 있는 Actor가 순서대로 스킬 우선 사용, 이동, 기본 공격을 수행하는 초안
- `StageController`: `RuntimeSetup` 단일 입력으로 스테이지 런타임을 초기화하고, 플레이어 생성/회복, 몬스터 생성, 처치 보상, 다음 스테이지 진행, 현재 Stage 재시작/외부 Stage 시작 요청 처리
- `StageSceneFlowController`: Field/Battle 모드 전환, 전투 스테이지 요청, 옵션 기반 씬 로드 처리
- `FieldEncounterController`: Player와 Encounter Point 거리가 가까워지면 전투 진입 요청
- `MonsterSpawner`: 몬스터 생성 타일과 반복 스폰 셀 오프셋 기반 스폰
- `HealthBarView`: 월드 공간 HP 바 갱신
- `CombatHud`: IMGUI 기반 보조 HUD

### Editor

경로: `Assets/IdleRPG/Scripts/Editor`

MVP 씬 구성과 검증을 위한 에디터 전용 코드다.

- `MvpSceneAutoLayout`: 씬 오픈, 활성 씬 변경, Play 진입 직전에 MVP 레이아웃 자동 재빌드 및 저장
- `MvpSceneSmokeTest`: 씬 슬롯, 필수 오브젝트, 필수 컴포넌트, 런타임 초기화 검증
- `Week1SceneBuilder`: `Week1VerticalSlice` 생성 메뉴 제공
- `ActorPrefabBuilder`: Actor/Monster 전용 prefab 생성 EditorWindow와 기본 생성 메뉴 제공
- `TileMapEditorWindow`: 행/열, 셀 크기, 시작/스폰 셀, fallback 색상, Sprite Palette, 칸별 시각 타입/통행 상태를 관리하는 타일맵 EditorWindow

## 런타임 흐름

1. `MvpSceneController.OnEnable()`에서 씬 레이아웃을 보정한다.
2. Play 모드 진입 시 `MvpSceneController.Awake()`가 다시 레이아웃을 확인한다.
3. `BattleContext`, `StageController`, `StageSceneFlowController`, `TurnBasedAutoBattleController`가 컨트롤러 오브젝트에 추가된다.
4. `MvpSceneController`의 `Game Content` 설정이 `RuntimeContentDatabase`를 만든다.
5. `StageController.Initialize(RuntimeSetup)`이 `TileMapLayout`과 `Player Start Point` 위치에 새 Hero를 풀 HP로 생성한 뒤 첫 몬스터를 스폰한다.
6. Player/Monster 정의의 Skill Loadout이 각 `ActorModel`에 `SkillRuntime`으로 복사된다.
7. `BattleContext.FindTarget()`이 `DefaultTargetSelector`로 적을 고르고, `AutoCombatController.Update()`가 사용 가능한 스킬을 먼저 실행한 뒤 월드 거리 기준 이동/공격을 수행한다.
8. 몬스터 사망 시 `StageController`가 보상을 지급하고 처치 수를 증가시킨다.
9. 처치 수가 요구치에 도달하면 다음 스테이지로 이동한다.
10. `MvpSceneController.Update()`가 HUD 텍스트와 HP Fill을 갱신한다.

턴제 자동전투를 시험하려면 `Designer Settings > Combat Loop > Mode`를 `TurnBased`로 바꾼다.
필드 조우를 시험하려면 `Designer Settings > Field Encounter > Enabled`를 켜고, `Scene Flow > Load Configured Scenes` 여부를 프로젝트 씬 분리 상태에 맞게 설정한다.

## 검증 결과

확인 완료 항목:

- Unity Roslyn 기반 스크립트 컴파일 체크 통과
- `MvpSceneController`에 `Game Content`와 `Designer Settings` 직렬화 설정 그룹 추가 확인
- `SampleScene`과 `Week1VerticalSlice`의 핵심 SerializeField 슬롯이 모두 non-zero fileID로 저장됨
- 씬 내 `Player Start Point`, `Monster Spawn Point`, `MVP HUD Canvas`, `Status Panel`, `Restart Panel`, `EventSystem` 존재 확인
- 기본 배치에 `Monster Actor`가 남지 않고 런타임 스폰 경로를 사용하는지 확인
- Actor 오브젝트의 `CombatActor`, `AutoCombatController`, `HealthBarView`, `Name Label` 존재 확인
- HUD의 Text/Image 구성 요소 존재 확인
- 스모크 테스트에 `StageController.Initialize(RuntimeSetup)` 실행, Player 슬롯 없이 Hero/Monster 런타임 모델 생성, Hero 사망 후 Stage 재시작 검증 추가
- 2026-08-24 변경 후 Unity Roslyn 기반 스크립트 컴파일 체크 통과
- 2026-08-24 batchmode 스모크 테스트는 원본 프로젝트가 이미 Unity 에디터에서 열려 있어 실행 차단됨
- 2026-08-26 Actor 프리팹 뼈대 생성 EditorWindow 추가 및 `Hero_Base`, `Monster_Base` prefab 구조 생성 확인
- 2026-08-26 임시 프로젝트에서 prefab 생성 메뉴와 기존 MVP 스모크 테스트 batchmode 검증 통과
- 2026-08-26 Actor/Monster prefab 생성 에디터 분리 및 prefab 스탯 프로필 직렬화 확인
- 2026-08-26 8x5 타일맵 생성, 타일 좌표 기반 Player/Monster 배치, 타일 이동/정렬 로직, 스모크 테스트 검증 통과
- 2026-08-27 사각형 타일 배치로 전환, 에디터 타일 그리드 들여쓰기 제거, y축 기준 sorting 검증 추가
- 2026-08-27 타일 관리 EditorWindow, `TileKind` 전역 enum, 막힌 칸 저장/렌더링/이동 회피 로직 추가
- 2026-08-27 `TileVisualKind` 전역 enum, Slice PNG용 Sprite Palette, Brush/Paint 기반 타일 시각 타입 편집 기능 추가
- 2026-08-27 기본 전투 이동과 적 탐색을 월드 좌표 기준으로 조정하고, 타일 이동은 옵션으로 남김
- 2026-08-28 `ActorStateMachine`, `StatModifier`, `TargetSelectionMode`, `DefaultTargetSelector`, `CombatRangePolicy` 추가
- 2026-08-28 `StageSceneFlowController`, `FieldEncounterController`, `TurnBasedAutoBattleController` 초안 추가
- 2026-08-28 `dotnet build Idle_RPG.sln` 통과
- 2026-08-28 임시 프로젝트 batchmode 스모크 테스트 통과: `SampleScene`, `Week1VerticalSlice`
- 2026-08-31 `CombatLoopMode`, `ICombatLoop` 추가 및 Realtime/TurnBased 전투 루프 배타 실행 구조 보강
- 2026-08-31 Field 모드에서 전투 Actor를 즉시 스폰하지 않고 Battle 요청 시 Stage를 시작하도록 경계 보강
- 2026-08-31 `IDamageCalculator`, `ISkillExecutor`, `ISkillEffect`, `StatModifierStack` 추가
- 2026-08-31 스크립트 역할 문서 `ScriptRoleGuide.md` 추가
- 2026-08-31 `dotnet build Idle_RPG.sln` 통과
- 2026-08-31 임시 프로젝트 batchmode 스모크 테스트 통과: `SampleScene`, `Week1VerticalSlice`
- 2026-09-02 `SkillDefinition`, `SkillRuntime`, `SkillLoadout`, `SkillExecutor`, `DamageSkillEffect`, `BuffSkillEffect` 추가
- 2026-09-02 `Game Content > Skills`와 Player/Monster `SkillLoadout` 설정 추가
- 2026-09-02 Realtime/TurnBased 전투 루프가 사거리 안의 ready skill을 기본 공격보다 먼저 실행하도록 연결
- 2026-09-02 스모크 테스트에 스킬 4 Slot 제한, 쿨다운 회복, 피해 효과, 버프 만료, 런타임 DamageTaken 이벤트 검증 추가
- 2026-09-02 `dotnet build Idle_RPG.sln` 통과
- 2026-09-02 임시 프로젝트 batchmode 스모크 테스트 통과: `SampleScene`, `Week1VerticalSlice`

주의:

- 원본 프로젝트가 이미 Unity 에디터에서 열려 있으면 같은 프로젝트를 batchmode로 동시에 열 수 없다.
- 이 때문에 최종 batch 검증은 원본 상태를 임시 복사한 프로젝트에서 수행했다.
- 원본 씬 파일에는 검증된 복사본의 Unity 저장 결과를 적용했다.

## 현재 한계

- 그래픽은 기본적으로 임시 생성 스프라이트 기반이지만, 타일은 Sprite Palette에 Slice PNG Sprite를 할당해 교체할 수 있다.
- 타일맵은 MVP용 코드 생성 타일과 전용 EditorWindow 기반이며, 아직 Unity Tilemap 에셋/브러시 파이프라인은 아니다.
- 콘텐츠 데이터는 Inspector에서 조절 가능하지만 아직 별도 ScriptableObject/CSV 파이프라인은 아니다.
- 스킬 코어는 들어갔지만, 스킬 전용 UI, 아이콘, 애니메이션, 투사체, 시전 로그는 아직 없다.
- 장비, 인벤토리, 저장/로드, 성장 시스템은 아직 없다.
- 전투 AI는 Inspector에서 Nearest/LowestHp/HighestAttack 타겟 기준을 바꿀 수 있지만, 위협도/파티 포지션 판단은 아직 없다.
- 필드 조우와 턴제 전투는 핵심 흐름 초안이며, 실제 플레이어 필드 조작/전투 전용 씬 연출은 아직 분리되지 않았다.
- Stage 밸런스는 테스트용 수치다.

## 다음 작업 제안

1. CSV 또는 ScriptableObject 기반 콘텐츠 파이프라인으로 Inspector 설정을 자산화
2. 스킬 아이콘, 시전 로그, 쿨다운 표시 UI 추가
3. 기본 공격도 공용 Action/Skill 파이프라인으로 통합할지 결정
4. 실제 필드 플레이어 이동, 조우 지점, BattleScene 전환 연출 구성
5. 실제 2D 캐릭터/몬스터 스프라이트와 애니메이션 연결
