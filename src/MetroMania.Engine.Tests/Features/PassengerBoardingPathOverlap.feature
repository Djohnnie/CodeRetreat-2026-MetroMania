Feature: Passenger Boarding When Segment Path Overlaps Another Station
  Verifies that passengers board and are delivered correctly when the tile path
  of a line segment physically passes through the grid location of another station
  on the same line.

  # ──────────────────────────────────────────────────────────────────────────────
  # Geometry (reproduced from a real run):
  #
  #   Triangle (7,2) ──── diagonal ────► (4,1) ← Rectangle sits here!
  #                                        │
  #                                   horizontal
  #                                        ▼
  #   Circle (1,1) ◄────────────────────────
  #
  # The ComputeSegmentWaypoints algorithm for Triangle(7,2)→Circle(1,1) produces:
  #   dx=-6, dy=-1 → diagLen=1, startStraight=2
  #   Waypoints: (7,2)→(5,2)→(4,1)→(1,1)
  #
  # The tile path becomes:
  #   idx 0  (7,2) Triangle
  #   idx 1  (6,2)
  #   idx 2  (5,2)
  #   idx 3  (4,1) ← Rectangle (phantom mid-segment!)
  #   idx 4  (3,1)
  #   idx 5  (2,1)
  #   idx 6  (1,1) Circle
  #   idx 7  (2,1)
  #   idx 8  (3,1)
  #   idx 9  (4,1) Rectangle (actual terminal)
  #
  # Before the fix, the next-station scan from Circle (idx 6) in direction -1 found
  # Rectangle at idx 3 instead of Triangle at idx 0.  The boarding check then used
  # Dijkstra(Rectangle→Triangle)=9 steps, so 3+9=12 ≠ 6 → passenger never boarded.
  # ──────────────────────────────────────────────────────────────────────────────

  Scenario: A passenger at Circle boards and reaches Triangle when the segment path overlaps Rectangle
    Given a level with a Triangle station at (7,2) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Circle station at (1,1) with a spawn delay of 0 days and passengers every 1 hours
    And a level with a Rectangle station at (4,1) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (7,2) and (1,1)
    And the runner will extend the first line from station (1,1) to station (4,1)
    And the runner will deploy a train on the first line at station (1,1)
    When the simulation runs for 50 hours
    Then the score should be at least 1

  Scenario: Multiple Triangle passengers are delivered over time confirming sustained boarding
    Given a level with a Triangle station at (7,2) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Circle station at (1,1) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (4,1) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (7,2) and (1,1)
    And the runner will extend the first line from station (1,1) to station (4,1)
    And the runner will deploy a train on the first line at station (1,1)
    When the simulation runs for 200 hours
    Then the score should be at least 5
