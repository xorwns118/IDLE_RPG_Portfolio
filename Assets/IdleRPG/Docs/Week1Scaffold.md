# Week 1 Scaffold

Notion 일정 기준 현재 목표는 `Week 1 - Vertical Slice`입니다.

## 구현 범위

- Unity Assembly Definition 및 폴더 구조 생성
- 순수 C# 전투 계산 계층과 Unity 런타임 계층 분리
- Player / Monster 공통 Actor 모델
- 자동 타겟 탐색, 접근, 기본 공격
- Damage / HP / Death 처리
- 간단한 MonsterSpawner 및 Stage 진행
- Play 모드에서 확인 가능한 IMGUI 전투 HUD

## 실행 방법

기본 `SampleScene`에는 `MVP Scene Controller`가 배치되어 있습니다. 씬을 열면 Player / Monster / Ground / Canvas HUD가 에디터에 구성되고, Play를 누르면 배치된 Actor 오브젝트의 `CombatActor`, `AutoCombatController`, `HealthBarView`가 초기화되어 전투가 시작됩니다.

원한다면 Unity 메뉴에서 `Idle RPG > Build Week 1 Demo Scene`을 실행해 `Assets/Scenes/Week1VerticalSlice.unity` 씬을 별도로 만들 수 있습니다.

## 다음 리팩터링 방향

- Week 2: `AutoCombatController`의 상태 전환을 StateMachine 클래스로 분리
- Week 3: 기본 공격을 SkillDefinition / SkillRuntime / SkillEffect 구조로 이전
- Week 4: `DemoContentFactory`의 하드코딩 데이터를 CSV Importer와 RuntimeContentDatabase로 교체
