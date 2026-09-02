# Script Role Guide

최종 갱신일: 2026-09-02

이 문서는 `Assets/IdleRPG/Scripts` 아래 각 스크립트가 어떤 책임을 갖는지 빠르게 읽기 위한 가이드다.

먼저 큰 흐름을 보려면 `MvpImplementationBrief.md`를 읽고, 특정 파일의 역할을 확인하려면 이 문서를 읽는다.

## 추천 읽기 순서

1. `Domain/GlobalEnums.cs`
2. `Domain/Actors/StatBlock.cs`
3. `Domain/Actors/ActorModel.cs`
4. `Domain/Skills/SkillDefinition.cs`
5. `Domain/Skills/SkillRuntime.cs`
6. `Domain/Skills/SkillLoadout.cs`
7. `Runtime/Configuration/MvpGameContentSettings.cs`
8. `Runtime/Combat/SkillExecutor.cs`
9. `Runtime/Bootstrap/MvpSceneController.cs`
10. `Runtime/Stages/StageController.cs`
11. `Runtime/Combat/BattleContext.cs`
12. `Runtime/Combat/AutoCombatController.cs`
13. `Runtime/Combat/TurnBasedAutoBattleController.cs`
14. `Runtime/Configuration/MvpSceneDesignerSettings.cs`
15. `Editor/MvpSceneSmokeTest.cs`

## Domain

Unity `GameObject`나 `MonoBehaviour`에 의존하지 않는 순수 게임 규칙 계층이다.

### `Domain/GlobalEnums.cs`

프로젝트 여러 계층에서 공유하는 enum을 한 곳에서 관리한다.

- `ActorTeam`: Player와 Monster 팀 구분
- `ActorState`: Idle, Search, Move, Attack, Skill, Hit, Dead 상태
- `TileKind`: Walkable, Blocked 통행 상태
- `TileVisualKind`: Ground, Wall, Tree, Water, Decoration 타일 시각 타입
- `TargetSelectionMode`: Nearest, LowestHp, HighestAttack 타겟 선택 기준
- `CombatLoopMode`: Realtime, TurnBased 전투 루프 선택
- `StageFlowMode`: Field, Battle 진행 모드
- `EncounterTriggerMode`: Manual, Distance 조우 트리거
- `MonsterSpawnSelectionMode`: 여러 스폰 위치를 순차/랜덤 중 어떤 방식으로 고를지 결정
- `SkillTargetType`: 스킬 대상이 Enemy인지 Self인지 구분
- `SkillEffectKind`: 스킬 효과가 Damage인지 Buff인지 구분

### `Domain/Actors/ActorModel.cs`

전투 Actor의 런타임 모델이다.

- Actor ID, 표시 이름, 팀, 현재 HP, 현재 상태를 가진다.
- `ReceiveBasicAttack()`에서 피해를 받고 HP/사망 이벤트를 발행한다.
- `ReceiveSkillAttack()`에서 스킬 피해 배율을 적용한 피해를 받는다.
- `SkillLoadout`을 들고 있으며 `Tick()`에서 버프 만료와 스킬 쿨다운을 함께 갱신한다.
- `ActorStateMachine`으로 상태 변경을 위임한다.
- `StatModifierStack`을 통해 버프/디버프형 스탯 변형을 적용한다.
- Unity 컴포넌트가 아니므로 오프라인 전투 시뮬레이션으로 확장하기 좋은 핵심 모델이다.

### `Domain/Actors/ActorStateMachine.cs`

Actor 상태 전환 규칙을 관리한다.

- `CanTransition()`으로 현재 상태에서 다음 상태로 갈 수 있는지 판단한다.
- Dead 상태에서는 다른 상태로 돌아가지 못하게 막는다.
- `StateChanged` 이벤트를 통해 상태 변화 감지가 가능하다.
- 현재는 MVP 규칙만 들어 있으며, Week3 이후 상태별 Enter/Exit 동작을 확장할 후보 지점이다.

### `Domain/Actors/StatBlock.cs`

Actor의 최종 전투 스탯을 표현하는 읽기 전용 값 객체다.

- Max HP, 공격력, 방어력, 사거리, 공격 간격, 이동속도, 치명타 확률, 치명타 배율을 가진다.
- 생성 시 음수나 0으로 깨질 수 있는 값은 안전 범위로 보정한다.
- `Scale()`은 스테이지별 몬스터 성장에 사용한다.
- `Apply()`는 `StatModifier`를 적용해 최종 스탯을 계산한다.

### `Domain/Actors/StatModifier.cs`

버프/디버프가 스탯을 어떻게 바꾸는지 나타내는 값 객체다.

- 각 스탯에 additive 값과 multiplier 값을 가진다.
- `Additive()`와 `Multiplier()` 헬퍼로 읽기 쉽게 생성할 수 있다.
- 직접 지속시간을 갖지 않고, 지속시간과 출처 관리는 `StatModifierEntry`가 맡는다.

### `Domain/Actors/StatModifierEntry.cs`

하나의 스탯 변형 출처를 나타낸다.

- `SourceId`로 버프 출처를 구분한다.
- `DurationSeconds`와 `RemainingSeconds`로 시간 제한 버프를 표현한다.
- `Tick()`으로 남은 시간을 줄이고 만료 여부를 확인한다.

### `Domain/Actors/StatModifierStack.cs`

여러 개의 `StatModifierEntry`를 모아 최종 modifier를 만든다.

- source id 기준 제거를 지원한다.
- 시간 제한 modifier의 만료 처리를 지원한다.
- additive는 합산하고 multiplier는 곱산한다.
- Week3의 `BuffEffect`가 연결될 핵심 확장 지점이다.

### `Domain/Skills/SkillDefinition.cs`

스킬의 정적 콘텐츠 데이터를 표현한다.

- Skill ID, 표시 이름, 대상 타입, 쿨다운, 사거리, 우선순위를 가진다.
- Damage/Buff 같은 여러 `SkillEffectDefinition`을 순서대로 가진다.
- 런타임 값은 저장하지 않고, 콘텐츠 원본 역할만 한다.

### `Domain/Skills/SkillEffectDefinition.cs`

스킬 안에 들어가는 개별 효과 데이터를 표현한다.

- `Damage` 효과는 공격력 배율을 가진다.
- `Buff` 효과는 `StatModifier`와 지속시간을 가진다.
- 각 효과마다 Enemy/Self 대상 타입을 별도로 지정할 수 있다.

### `Domain/Skills/SkillRuntime.cs`

스킬 1개의 런타임 상태를 관리한다.

- `SkillDefinition`을 참조하고 남은 쿨다운 시간을 가진다.
- `Tick()`, `StartCooldown()`, `ResetCooldown()`으로 쿨다운을 관리한다.
- 같은 스킬 정의라도 Actor마다 별도 런타임 상태를 갖게 해준다.

### `Domain/Skills/SkillLoadout.cs`

Actor가 장착한 스킬 슬롯 묶음이다.

- 최대 4칸만 허용한다.
- 각 슬롯은 `SkillRuntime`으로 저장된다.
- `SelectBestReadySkill()`은 사용 가능한 스킬 중 Priority가 가장 높은 것을 고른다.

### `Domain/Combat/DamageResult.cs`

피해 계산 결과를 담는 값 객체다.

- 최종 피해량과 치명타 여부를 가진다.
- `DamageResult.None`은 피해가 없는 결과를 나타낸다.

### `Domain/Combat/IDamageCalculator.cs`

피해 계산 구현체가 따라야 하는 인터페이스다.

- 현재는 기본 공격 계산만 정의한다.
- 이후 스킬 피해, 방어 타입, 속성, 관통 등을 분리할 때 확장한다.

### `Domain/Combat/BasicDamageCalculator.cs`

현재 기본 공격 피해 공식을 구현한다.

- 공격력에서 방어력을 뺀 뒤 최소 피해 1을 보장한다.
- 치명타 확률과 치명타 배율을 적용한다.
- `CombatMath`가 기본 계산기로 사용한다.

### `Domain/Combat/CombatMath.cs`

기존 호출부를 유지하기 위한 정적 facade다.

- 내부적으로 `BasicDamageCalculator`에 위임한다.
- 기존 코드가 `CombatMath.CalculateBasicAttack()`을 계속 호출해도 새 계산 구조를 사용할 수 있게 한다.

### `Domain/Combat/ISkillExecutor.cs`

Skill System 실행 구현체가 따라야 하는 인터페이스다.

- `CanExecute()`로 스킬 사용 가능 여부와 사거리 조건을 판단한다.
- `Execute()`로 스킬 실행을 수행한다.
- Domain 모델만으로 스킬 실행을 테스트할 수 있게 한다.

### `Domain/Combat/ISkillEffect.cs`

Skill Effect 조합을 위한 인터페이스다.

- `Apply()`로 Damage, Buff, Move 같은 효과를 ActorModel에 적용한다.
- 상속형 스킬보다 Effect 조합형 구조로 확장하기 위한 시작점이다.

### `Domain/Combat/SkillExecutionResult.cs`

스킬 실행 결과를 담는다.

- 성공 여부, 스킬 ID, 표시 이름, 적용된 효과 수, 마지막 피해 결과를 가진다.
- `SkillEffectResult`도 같은 파일에 있으며 개별 효과의 적용 결과를 나타낸다.

### `Domain/Combat/DamageSkillEffect.cs`

Damage 효과를 ActorModel에 적용한다.

- 스킬 효과의 공격력 배율을 `ActorModel.ReceiveSkillAttack()`에 전달한다.
- 최종 피해량 계산은 기존 combat 계산 흐름을 재사용한다.

### `Domain/Combat/BuffSkillEffect.cs`

Buff 효과를 ActorModel에 적용한다.

- 같은 시전자/스킬/효과 출처의 기존 modifier를 제거한 뒤 새 modifier를 적용한다.
- `StatModifierStack` 지속시간 만료 처리와 연결된다.

### `Domain/Data/PlayerDefinition.cs`

플레이어의 정적 콘텐츠 데이터를 표현한다.

- ID, 표시 이름, 기본 스탯, Skill Loadout을 가진다.
- 런타임 HP나 상태는 저장하지 않는다.

### `Domain/Data/MonsterDefinition.cs`

몬스터의 정적 콘텐츠 데이터를 표현한다.

- ID, 표시 이름, 기본 스탯, Gold/EXP 보상, Skill Loadout을 가진다.
- `WithStats()`로 스테이지 스케일링된 임시 정의를 만들 수 있다.

### `Domain/Data/StageDefinition.cs`

스테이지 콘텐츠 데이터를 표현한다.

- Stage 번호, 등장 몬스터 ID, 클리어에 필요한 처치 수를 가진다.

### `Domain/Data/RuntimeContentDatabase.cs`

런타임에서 Player, Skill, Monster, Stage definition을 조회하는 데이터베이스다.

- ID 기반 Skill/Monster 조회를 담당한다.
- Stage 번호가 정의 범위를 넘어가면 마지막 Stage 정의를 재사용한다.

## Runtime

Unity 씬에서 실제로 동작하는 계층이다.

### `Runtime/Bootstrap/GameBootstrap.cs`

초기 부트스트랩용 자리다.

- 현재 MVP에서는 `MvpSceneController`가 실제 씬 초기화를 담당한다.
- 추후 앱 전체 초기화, 서비스 등록, 첫 씬 로드 진입점으로 확장할 수 있다.

### `Runtime/Bootstrap/GeneratedSpriteFactory.cs`

MVP용 임시 스프라이트를 코드로 생성한다.

- Actor용 단색 유닛 스프라이트를 만든다.
- 사각형 타일 fallback 스프라이트를 만든다.
- 실제 아트 에셋이 들어오면 사용 비중이 줄어든다.

### `Runtime/Bootstrap/MvpSceneController.cs`

현재 MVP 씬의 메인 진입점이다.

- `Awake()`에서 씬 레이아웃을 확인하고 런타임을 시작한다.
- Camera, World, TileMap, HUD, Restart Panel, EventSystem을 구성한다.
- `BattleContext`, `StageController`, `StageSceneFlowController`, `TurnBasedAutoBattleController`를 연결한다.
- `CombatLoopMode`에 따라 Realtime 또는 TurnBased 전투가 동시에 실행되지 않도록 조정한다.
- Field 모드에서는 즉시 전투 Stage를 만들지 않고, Battle 요청이 들어오면 Stage를 시작한다.

### `Runtime/Configuration/MvpGameContentSettings.cs`

Inspector에서 조절 가능한 콘텐츠 데이터 설정이다.

- Player, Skill, Monster, Stage 기본 데이터를 가진다.
- Player/Monster별 4 Slot Skill Loadout을 Inspector 문자열 ID로 설정한다.
- Skill에는 쿨다운, 사거리, 우선순위, Damage/Buff 효과 배열을 설정한다.
- `CreateDatabase()`로 Domain의 `RuntimeContentDatabase`를 만든다.
- 아직 CSV나 ScriptableObject 파이프라인은 아니다.

### `Runtime/Configuration/MvpSceneDesignerSettings.cs`

Inspector에서 조절 가능한 씬/전투/표시 설정 묶음이다.

- Camera, World Layout, Tile Map, Actor View, Combat Loop, Targeting, Monster Spawn, Scene Flow, Field Encounter, Turn Combat, HUD, Restart Panel, Stage Runtime 설정을 가진다.
- 디자이너가 코드 수정 없이 MVP 수치를 조절하는 중심 파일이다.
- 파일이 커지고 있으므로 이후 ScriptableObject나 설정 파일 분리가 필요하다.

### `Runtime/Data/DemoContentFactory.cs`

초기 Week1 데이터 호환용 팩토리다.

- 기본 플레이어, 슬라임, Stage 데이터를 생성한다.
- 현재는 `MvpGameContentSettings`가 메인이고, 이 파일은 테스트/호환 경로에 가깝다.

### `Runtime/Actors/ActorFactory.cs`

Actor GameObject를 실제 전투 Actor로 구성한다.

- `CombatActor`, `HealthBarView`, `Name Label`, `AutoCombatController`를 붙인다.
- Player/Monster 팀에 따라 Scale, Color, SortingOrder를 적용한다.
- `CombatLoopMode.TurnBased`일 때는 per-actor `AutoCombatController`를 비활성화한다.

### `Runtime/Actors/ActorPrefabProfile.cs`

에디터에서 생성한 Actor/Monster prefab에 저장되는 프로필이다.

- ID, 표시 이름, 팀, 색상, 스탯, 몬스터 보상을 Inspector에 노출한다.
- Prefab 기반 콘텐츠 제작 흐름의 시작점이다.

### `Runtime/Actors/CombatActor.cs`

Unity GameObject와 Domain `ActorModel`을 이어주는 런타임 컴포넌트다.

- SpriteRenderer 색상과 방향 전환을 관리한다.
- 피격/사망 이벤트를 Unity 쪽으로 전달한다.
- 기본 공격과 스킬 공격 모두 `DamageTaken` 이벤트를 발행한다.
- 실제 전투 계산은 `ActorModel`과 Domain combat 로직에 맡긴다.

### `Runtime/Combat/BattleContext.cs`

현재 전투에 참여 중인 Actor 목록과 타겟팅 진입점을 관리한다.

- Actor 등록/해제를 담당한다.
- Player 참조와 TileMap 참조를 가진다.
- `FindTarget()`에서 `ITargetSelector`를 통해 타겟을 선택한다.
- `TickActors()`로 전투 중인 Actor의 버프 지속시간과 스킬 쿨다운을 갱신한다.
- 몬스터 사망 후 짧은 지연 뒤 오브젝트를 제거한다.

### `Runtime/Combat/SkillExecutor.cs`

런타임 스킬 실행을 담당한다.

- Actor의 `SkillLoadout`에서 ready 상태이고 사거리 조건을 만족하는 최고 Priority 스킬을 고른다.
- Damage 효과는 `CombatActor.TakeSkillAttack()`을 사용해 HUD/로그 이벤트가 유지되게 한다.
- Buff 효과는 Domain의 `BuffSkillEffect`를 사용해 스탯 modifier로 적용한다.
- Realtime과 TurnBased 전투 루프가 공통으로 사용한다.

### `Runtime/Combat/ITargetSelector.cs`

타겟 선택 구현체의 인터페이스다.

- 후보 Actor 목록과 targeting 설정을 받아 최종 타겟을 반환한다.
- 이후 도발, 위협도, 파티 포지션, 우선순위 조건을 추가할 때 교체 지점이 된다.

### `Runtime/Combat/DefaultTargetSelector.cs`

현재 기본 타겟 선택 구현이다.

- 같은 팀과 죽은 Actor는 제외한다.
- Search Range 밖의 적은 설정에 따라 제외한다.
- Nearest, LowestHp, HighestAttack 기준으로 타겟을 고른다.

### `Runtime/Combat/CombatRangePolicy.cs`

전투 거리와 사거리 판정을 담당한다.

- 월드 좌표 거리 기준으로 공격 가능 여부를 계산한다.
- 공격 범위 padding을 적용한다.
- 접근 시 멈출 위치를 계산한다.

### `Runtime/Combat/ICombatLoop.cs`

전투 루프 컴포넌트가 공유하는 인터페이스다.

- Realtime과 TurnBased 루프의 모드와 활성 상태를 노출한다.
- `SetRuntimeActive()`로 런타임에서 한쪽 루프만 켜도록 제어한다.

### `Runtime/Combat/AutoCombatController.cs`

Realtime 방식의 per-actor 자동 전투 루프다.

- 매 프레임 타겟을 찾고 Actor의 버프/스킬 쿨다운을 갱신한다.
- 사용 가능한 스킬이 있으면 기본 공격보다 먼저 실행한다.
- 스킬 사거리와 기본 공격 사거리 밖이면 이동한다.
- 사거리 안이면 기본 공격을 수행한다.
- `UseTileMovement`를 켜면 타일 셀 기반 이동을 사용할 수 있다.
- `ICombatLoop`을 구현하며 TurnBased 모드에서는 비활성화된다.

### `Runtime/Combat/TurnBasedAutoBattleController.cs`

TurnBased 방식의 stage-level 자동 전투 루프다.

- 일정 Turn Delay마다 살아 있는 Actor를 순서대로 선택한다.
- 선택된 Actor가 타겟을 찾아 사용 가능한 스킬을 먼저 실행한다.
- 스킬을 사용할 수 없으면 이동하거나 기본 공격을 수행한다.

### `Runtime/Maps/TileMapLayout.cs`

사각형 타일맵의 런타임 배치와 좌표 계산을 담당한다.

- 설정에 맞춰 타일 시각 요소를 생성한다.
- Cell/World 좌표 변환을 제공한다.
- 막힌 칸 판정과 가까운 walkable cell 탐색을 제공한다.
- y축 기반 sorting order를 계산해 2.5D 느낌의 겹침 순서를 만든다.

### `Runtime/Stages/StageController.cs`

스테이지 전투 진행을 담당한다.

- `RuntimeSetup`으로 필요한 의존성을 한 번에 받는다.
- Stage 시작, Hero 생성/회복, Monster 스폰, 처치 보상, 다음 Stage 진행, Stage 재시작을 처리한다.
- `StartStage()`로 외부 Battle 요청을 받을 수 있다.
- `ClearRuntime()`으로 Field 모드 진입 시 전투 Actor를 제거한다.

### `Runtime/Stages/MonsterSpawner.cs`

몬스터 생성 위치를 계산하고 몬스터 Actor를 만든다.

- 기본 Spawn Point를 사용한다.
- TileMap이 있으면 타일 셀 오프셋 기반으로 반복 스폰 위치를 계산한다.
- 막힌 칸이면 가까운 walkable cell을 찾는다.
- MonsterDefinition의 Skill Loadout을 스폰된 ActorModel에 복사한다.

### `Runtime/Stages/StageSceneFlowController.cs`

Field/Battle 흐름 전환을 담당한다.

- 현재 모드와 요청된 Battle Stage 번호를 관리한다.
- `LoadConfiguredScenes`가 켜져 있으면 설정된 씬 이름으로 `SceneManager.LoadScene()`을 호출한다.
- 씬 분리가 아직 없을 때는 같은 씬 안에서 Battle 요청만 전달하는 역할도 한다.

### `Runtime/Stages/FieldEncounterController.cs`

필드 조우 트리거를 담당한다.

- Player Transform과 Encounter Point 거리를 검사한다.
- Trigger Distance 안으로 들어오면 `StageSceneFlowController.EnterBattle()`을 호출한다.
- `TriggerOnce`로 중복 조우를 막을 수 있다.

### `Runtime/UI/HealthBarView.cs`

Actor 머리 위 월드 HP 바를 관리한다.

- `ActorModel.HealthChanged` 이벤트를 받아 fill scale을 갱신한다.
- TileMap sorting과 함께 표시 순서를 보정할 수 있다.

### `Runtime/UI/CombatHud.cs`

IMGUI 기반 보조 HUD다.

- 초기 디버그/보조 표시용 성격이 강하다.
- 현재 메인 MVP HUD는 `MvpSceneController`가 생성하는 Canvas UI다.

## Editor

Unity 에디터에서 씬/프리팹/타일맵을 만들고 검증하기 위한 코드다.

### `Editor/ActorPrefabBuilder.cs`

Actor와 Monster prefab 뼈대를 만드는 EditorWindow다.

- Actor/Monster 생성 메뉴를 분리한다.
- 스탯과 보상 값을 Inspector에서 세팅할 수 있는 prefab profile을 만든다.
- 기본 Actor/Monster prefab 생성 메뉴도 제공한다.

### `Editor/MvpSceneAutoLayout.cs`

씬 열기, 활성 씬 변경, Play 진입 전에 MVP 레이아웃을 자동 보정한다.

- `MvpSceneController.RebuildSceneLayout()`을 호출한다.
- 씬에 필요한 참조 슬롯이 비어 있는 문제를 줄인다.

### `Editor/MvpSceneSmokeTest.cs`

MVP 씬과 런타임 구조를 검증하는 에디터 테스트 메뉴다.

- `SampleScene`과 `Week1VerticalSlice`의 필수 오브젝트/컴포넌트/SerializeField 슬롯을 확인한다.
- 타일맵, HUD, Restart Panel, Runtime Stage Boot를 검증한다.
- Realtime/TurnBased 루프 배타성, Field Encounter, StateMachine, StatModifier도 검증한다.
- SkillDefinition/SkillRuntime/SkillLoadout, DamageEffect, BuffEffect, 런타임 DamageTaken 이벤트도 검증한다.

### `Editor/TileMapEditorWindow.cs`

타일맵 설정과 셀 페인팅을 관리하는 EditorWindow다.

- Columns, Rows, Cell Size, Origin, Player Start Cell, Monster Spawn Cell을 조절한다.
- Sprite Palette에 Slice PNG Sprite를 연결한다.
- Brush/Paint 방식으로 Ground, Wall, Tree, Water, Decoration을 칠한다.
- Wall/Tree/Water는 기본 Blocked로 처리된다.

### `Editor/Week1SceneBuilder.cs`

Week1 Vertical Slice 씬 생성 메뉴다.

- Week1VerticalSlice 씬을 만들고 MVP 컨트롤러 레이아웃을 구성한다.
- 초기 씬 복구나 데모 씬 재생성에 사용한다.

### `Editor/IdleRPG.Editor.asmdef`

Editor 전용 어셈블리 정의 파일이다.

- Editor 폴더 스크립트를 런타임 빌드와 분리한다.
- UnityEditor API를 런타임 어셈블리에서 참조하지 않게 막는다.

## 지금 구조에서 특히 먼저 읽을 파일

전투 흐름을 이해하려면 아래 네 개를 먼저 보면 된다.

- `Runtime/Bootstrap/MvpSceneController.cs`
- `Runtime/Stages/StageController.cs`
- `Runtime/Combat/BattleContext.cs`
- `Runtime/Combat/AutoCombatController.cs`

Week3 Skill System을 준비하려면 아래 파일을 같이 보면 된다.

- `Domain/Skills/SkillDefinition.cs`
- `Domain/Skills/SkillRuntime.cs`
- `Domain/Skills/SkillLoadout.cs`
- `Domain/Skills/SkillEffectDefinition.cs`
- `Domain/Combat/SkillExecutionResult.cs`
- `Domain/Combat/DamageSkillEffect.cs`
- `Domain/Combat/BuffSkillEffect.cs`
- `Domain/Combat/ISkillExecutor.cs`
- `Domain/Combat/ISkillEffect.cs`
- `Domain/Combat/IDamageCalculator.cs`
- `Domain/Actors/StatModifierStack.cs`
- `Runtime/Combat/SkillExecutor.cs`
- `Runtime/Combat/TurnBasedAutoBattleController.cs`
