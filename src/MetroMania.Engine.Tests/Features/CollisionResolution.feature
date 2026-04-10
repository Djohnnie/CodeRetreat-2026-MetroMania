Feature: Collision Resolution
  Verifies the three-phase collision resolution rules in the engine:
    Rule A — A train cannot enter a station tile occupied by a working/blocked train.
    Rule B — A train cannot enter a non-station tile occupied by a same-direction blocked train.
    Rule C — When two moving trains simultaneously target the same station, the lower-index train wins.

  # ──────────────────────────────────────────────────────────────────────
  # Rule A: station occupation blocks incoming train
  #
  # Train 1 deployed at Rectangle(2,0) picks up passengers (stays).
  # Train 2 deployed at Circle(0,0) moves toward (2,0) but is blocked.
  # Timeline: tick 0 create line, tick 1 deploy T1@(2,0), tick 2 deploy T2@(0,0),
  # tick 3 T2 moves to (1,0), tick 4 T2 tries (2,0) → blocked by Rule A.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A train is blocked from entering a station occupied by a working train (Rule A)
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (2,0) with a spawn delay of 0 days and passengers every 1 hour
    And the level has 1 initial Line and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (2,0)
    And the runner will deploy a train on the first line at station (2,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 5 hours
    Then train 2 should be at tile (1,0)
    And train 2 should not be at tile (2,0)

  # ──────────────────────────────────────────────────────────────────────
  # Rule C: simultaneous station arrival on the same line — lower index wins
  #
  # Line 1: Triangle(0,0) → Rectangle(4,0) → Circle(7,0).
  # Train 1 deployed at Triangle(0,0) — 4 tiles from Rectangle.
  # Train 2 deployed at Circle(7,0) 1 tick later — immediately reverses (dir=-1) —
  #   3 tiles from Rectangle.
  # Both trains are on the same line and arrive at Rectangle(4,0) on the same tick.
  # Train 1 (lower index) wins; Train 2 is blocked at (5,0).
  # ──────────────────────────────────────────────────────────────────────

  Scenario: Two trains on the same line simultaneously targeting the same station — lower index wins (Rule C)
    Given a level with a Triangle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Circle station at (7,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will extend line 1 from station (4,0) to station (7,0)
    And the runner will deploy a train on line 1 at station (0,0)
    And the runner will deploy a train on line 1 at station (7,0)
    When the simulation runs for 7 hours
    Then train 1 should be at tile (4,0)
    And train 2 should not be at tile (4,0)

  # ──────────────────────────────────────────────────────────────────────
  # Cross-line independence: trains on different lines share tile segments
  # independently (like separate tracks) and must NOT block each other.
  #
  # Line 1: Circle(0,0) → Rectangle(4,0). Line 2: Triangle(7,0) → Rectangle(4,0).
  # Both share the Rectangle(4,0) endpoint.
  # Train 1 (Line 1, from (0,0), 4 tiles away) and Train 2 (Line 2, from (7,0),
  # 3 tiles away, deployed 1 tick later) both arrive at Rectangle on the same tick.
  # Because they are on different lines they are independent — both stop at the
  # shared station simultaneously without blocking each other.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: Trains on different lines may simultaneously occupy the same station (cross-line independence)
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (4,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (7,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 2 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (4,0)
    And the runner will create a second line between stations at (7,0) and (4,0)
    And the runner will deploy a train on line 1 at station (0,0)
    And the runner will deploy a train on line 2 at station (7,0)
    When the simulation runs for 7 hours
    Then train 1 should be at tile (4,0)
    And train 2 should be at tile (4,0)

  # ──────────────────────────────────────────────────────────────────────
  # Rules A + B cascade: 3 trains on one line. Train 1 does pickup work
  # at Rectangle(3,0). Train 2 is blocked from entering (3,0) by Rule A,
  # stays on open track (2,0). Train 3 is blocked from entering (2,0) by
  # Rule B (same direction, non-station tile).
  #
  # Line: Circle(0,0) → Rectangle(3,0) → Triangle(6,0).
  # Tick 0: create line (0,0)→(3,0). Tick 1: extend to (6,0).
  # Tick 2: deploy T1 at (0,0). Tick 3: deploy T2 at (0,0), T1 moves to (1,0).
  # Tick 4: deploy T3 at (0,0), T1→(2,0), T2→(1,0).
  # Tick 5: T1→(3,0), T2→(2,0), T3→(1,0).
  # Tick 6: T1 stays (3,0) picking up; T2 blocked at (2,0) Rule A; T3 blocked at (1,0) Rule B.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: Blocking cascades from station to trailing train on open track (Rules A + B)
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Triangle station at (6,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 3 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 7 hours
    Then train 1 should be at tile (3,0)
    And train 2 should be at tile (2,0)
    And train 3 should be at tile (1,0)
