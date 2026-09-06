# A.D.D. Gameplay Push Resolver

A.D.D. (Anomaly Detection Department)의 보드 겹침 해소 로직 중
점유 상태 기반 Push Solver를 포트폴리오 공개용으로 분리한 저장소입니다.

겹친 Pair를 하나씩 반복 분리하던 처리를
현재 점유 상태에서 빈 공간 후보와 이동 경로를 먼저 찾는 문제로 재정의했습니다.

```text
반복 Pair Separation
  → Occupancy Grid + Vacancy Search
  → Push Planning + Validation
  → 최종 위치 적용
```

전체 게임 프로젝트와 Unity View 코드는 포함하지 않으며,
Solver에 필요한 로직만 별도의 .NET 프로젝트로 분리했습니다.

## 이 코드에서 보여주고 싶은 것

- 연쇄 Push를 개별 충돌의 반복이 아닌 빈 공간 탐색 문제로 다루는 방법
- 실제 크기와 margin을 반영한 임시 Occupancy Grid 구성
- 방향 우선순위를 반영한 BFS와 parent 경로 복원
- 벽과 코너에서 대체 계획을 찾는 bounded search와 backtracking
- 계산된 이동 계획을 검증한 뒤에만 결과를 반환하는 구조
- Unity와 게임 상태 관리 코드에서 독립된 Solver API

## 문제: 반복되는 Pair Separation

기존 방식은 겹친 Pair를 찾고 두 오브젝트를 분리한 뒤,
변경된 위치에서 주변 Pair를 다시 검사했습니다.

```text
Pair 탐색
  → 분리
  → 새 겹침 검사
  → 재처리
```

연쇄 Push가 길어질수록 한 번의 위치 변경이 주변 충돌로 전파되고,
같은 영역의 검사와 재배치가 반복될 수 있습니다.

이를 다음 질문으로 바꿨습니다.

> 현재 점유 상태에서 도달 가능한 빈 공간은 어디이며,
> 그 공간까지 어떤 순서로 오브젝트를 이동할 수 있는가?

## 해결 흐름

### 1. 임시 Occupancy Grid를 구성합니다

[`OccupancyGrid.cs`](src/Occupancy/OccupancyGrid.cs)는 현재 보드 상태를
Push 계획에 사용할 임시 Grid로 변환합니다.

- 움직일 수 있는 Item의 점유 상태 기록
- 고정 Item이 막는 Cell 기록
- 실제 `HalfSize`와 separation margin 반영
- 탐색 중인 Item 크기에 맞춰 Cell 점유 범위 계산

Grid는 실제 보드 위치를 직접 변경하지 않습니다.
탐색과 계획을 위한 임시 상태로만 사용합니다.

### 2. 방향 우선순위를 반영해 빈 공간을 찾습니다

[`VacancyPlanning.cs`](src/Occupancy/VacancyPlanning.cs)는 Grid를 구성하고
[`VacancyRoutes.cs`](src/Occupancy/VacancyRoutes.cs)는 점유 영역 가장자리의 빈 공간 후보를 수집합니다.

탐색은 BFS의 이웃 방문 순서에 사용자가 밀어낸 방향의 우선순위를 반영하고,
연결된 점유 영역을 따라 인접 Cell로 확장합니다.

각 탐색 경로에는 parent Cell을 기록해
빈 공간에서 시작점까지의 경로를 다시 구성할 수 있도록 했습니다.

### 3. 경로를 이동 방향으로 사용해 Push 계획을 만듭니다

[`BoardPushSolver.cs`](src/BoardPushSolver.cs)가 입력 Snapshot과 기준 Item을 받아
전체 해결 과정을 시작합니다.

[`BoundedPushPlanner.cs`](src/Planning/BoundedPushPlanner.cs)는 복원된 경로를 방향 가이드로 사용해
겹친 Item의 목표 위치와 다음 Push 대상을 순서대로 계산합니다.

기본 계획이 벽이나 코너에서 막히면 다음 대체 경로를 시도합니다.

- [`BacktrackingPlanner.cs`](src/Planning/Recovery/BacktrackingPlanner.cs): 이전 계획 상태로 돌아가 다른 이동 조합 탐색
- [`CornerPackingPlanner.cs`](src/Planning/Recovery/CornerPackingPlanner.cs): 코너에 연결된 Item의 재배치 계획
- [`MultiRootPlanner.cs`](src/Planning/Recovery/MultiRootPlanner.cs): 여러 방향으로 겹친 기준 Item 처리

탐색 후보 수, 상태 수, 깊이와 재시도 횟수에는 상한을 둡니다.

### 4. 유효한 계획만 결과로 반환합니다

[`PlanValidation.cs`](src/Planning/State/PlanValidation.cs)는 계산된 최종 위치를 적용하기 전에
다음 조건을 다시 확인합니다.

- 기준 Item의 위치 유지
- Item 사이의 유효 겹침 제거
- 고정 Item의 위치 유지
- 실제 크기와 margin 반영
- 설정된 경우 보드 경계 내부 유지

검증에 성공하면 모든 Item의 최종 위치를 반환합니다.
해결할 수 없으면 부분 이동 결과를 노출하지 않고 실패를 반환합니다.

## 사용 예시

```csharp
using System.Numerics;
using ADD.Gameplay.PushResolver;

var items = new[]
{
    new BoardItem(1, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f)),
    new BoardItem(2, new Vector2(0.6f, 0f), new Vector2(0.5f, 0.5f))
};

var settings = new BoardPushSolverSettings(
    padding: 0.08f,
    clampToBoardBounds: true,
    boardPadding: 0.1f);

var solver = new BoardPushSolver(settings);
var boardBounds = new Aabb2(
    new Vector2(-5f, -5f),
    new Vector2(5f, 5f));

var result = solver.Solve(
    items,
    boardBounds,
    pinnedId: 1,
    preferredDirection: Vector2.UnitX,
    directionPolicy: ResolveDirectionPolicy.TryAlternates);

if (result.Succeeded)
{
    var finalPosition = result.Positions[2];
}
```

`Solve`는 입력 `BoardItem`을 변경하지 않습니다.
성공한 경우에만 Item ID별 최종 위치를 읽기 전용 결과로 제공합니다.

## 프로젝트 구조

```text
ADD-Gameplay-Push-Resolver
├─ src
│  ├─ BoardPushSolver.cs
│  ├─ BoardItem.cs
│  ├─ BoardPushSolverSettings.cs
│  ├─ PushResult.cs
│  ├─ Geometry
│  │  ├─ Aabb2.cs
│  │  ├─ BoardBoundary.cs
│  │  └─ SpatialGrid.cs
│  ├─ Occupancy
│  │  ├─ OccupancyGrid.cs
│  │  ├─ VacancyPlanning.cs
│  │  └─ VacancyRoutes.cs
│  └─ Planning
│     ├─ BoundedPushPlanner.cs
│     ├─ LocalCascadePlanner.cs
│     ├─ State
│     └─ Recovery
│
└─ tests/ADD.Gameplay.PushResolver.Tests
   ├─ StraightPushTests.cs
   ├─ CascadePushTests.cs
   ├─ BfsDetourTests.cs
   ├─ WallBlockedTests.cs
   ├─ DifferentSizeTests.cs
   └─ NoVacancyTests.cs
```

## 테스트

NUnit 테스트는 공개된 `BoardPushSolver.Solve` API를 기준으로 작성했습니다.

- [`StraightPushTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/StraightPushTests.cs): 전방 빈 공간으로 이동
- [`CascadePushTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/CascadePushTests.cs): 연결된 Item의 연쇄 이동
- [`BfsDetourTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/BfsDetourTests.cs): 전방이 막힌 경우 측면 빈 공간 사용
- [`WallBlockedTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/WallBlockedTests.cs): 보드 벽에서 대체 방향 선택
- [`DifferentSizeTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/DifferentSizeTests.cs): 서로 다른 크기와 margin 반영
- [`NoVacancyTests.cs`](tests/ADD.Gameplay.PushResolver.Tests/NoVacancyTests.cs): 빈 공간이 없을 때 안전하게 실패

각 성공 시나리오는 최종 겹침, 보드 경계, 기준 Item과 고정 Item의 위치,
입력 데이터 불변 여부를 함께 확인합니다.

## 실행 방법

.NET 9 SDK가 필요합니다.

```bash
dotnet build src/ADD.Gameplay.PushResolver.csproj -c Release
dotnet test tests/ADD.Gameplay.PushResolver.Tests/ADD.Gameplay.PushResolver.Tests.csproj -c Release
```

## 공개용 추출

실제 프로젝트의 Resolver에는 입력 처리, View Bounds 수집, 게임 상태 조회,
Command 실행과 기존 Pair Separation이 함께 연결되어 있습니다.

이 저장소에서는 다음 의존성을 제외했습니다.

- Unity `Vector2`, MonoBehaviour, ScriptableObject
- Gameplay Command와 Read Model
- Product Tray, Trash Can과 View 이동 처리
- Scheduler와 View Bounds Registry
- 기존 Pair Separation 실행 경로

좌표는 `System.Numerics.Vector2`로 교체하고,
Occupancy, Planning, Validation, Recovery 책임을 독립된 파일로 분리했습니다.
