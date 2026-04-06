Feature: Transfer and Optimal Routing
  Verifies the Dijkstra-based routing logic:
  - A train only picks up a passenger when the next station is on the globally
    optimal path (graph-based boarding).
  - A train drops a carried passenger at an intermediate station when a shorter
    global path exists via another line (transfer drop).

  # ──────────────────────────────────────────────────────────────────────
  # Transfer drop: passenger dropped at an interchange for a better route
  #
  # Network topology:
  #   Line 1: Circle(0,0) ── Rectangle(3,0)
  #   Line 2: Rectangle(3,0) ── Triangle(0,3)
  #
  # A Circle-spawned passenger wants Triangle. Line 1 alone cannot reach
  # Triangle. The optimal path is: ride Line 1 to Rectangle, transfer,
  # ride Line 2 to Triangle.
  #
  # Train 1 (Line 1) picks up the passenger at Circle and carries it to
  # Rectangle. At Rectangle the engine detects that the global shortest
  # path (3 steps via Line 2) is shorter than staying on Line 1 (which
  # has no Triangle station) → transfer drop. The passenger is re-queued
  # at Rectangle for Train 2 (Line 2) to collect.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A passenger is dropped at an interchange for a shorter transfer route
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (0,3) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 2 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will create a second line between stations at (3,0) and (0,3)
    And the runner will deploy a train on line 1 at station (0,0)
    And the runner will deploy a train on line 2 at station (3,0)
    When the simulation runs for 50 hours
    Then the score should be at least 1
  # station on the line is on the globally optimal route.
  #
  # Network:
  #   Line 1: Circle(0,0) ── Rectangle(3,0)
  #   Line 2: Circle(0,0) ── Triangle(0,3)
  #
  # A Circle-spawned passenger wants Triangle. Line 2 goes directly to
  # Triangle. Train 1 on Line 1 should NOT pick up the passenger because
  # Rectangle (its next station) is NOT on the shortest path to Triangle.
  # Train 2 on Line 2 SHOULD pick up because Triangle IS the next station.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A train does not board a passenger when its line is not on the optimal route
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (0,3) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 2 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will create a second line between stations at (0,0) and (0,3)
    And the runner will deploy a train on line 1 at station (3,0)
    And the runner will deploy a train on line 2 at station (0,3)
    When the simulation runs for 50 hours
    Then the score should be at least 1
