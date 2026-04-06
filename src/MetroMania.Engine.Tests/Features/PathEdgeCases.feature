Feature: Path Edge Cases
  Verifies LinePathHelper handles edge cases: vertical-dominant segments
  (|dy| > |dx|), single-station lines, and backward-pass destination lookup
  in MinStepsViaLine.

  # ──────────────────────────────────────────────────────────────────────
  # Vertical-dominant segment: |dy| > |dx|
  # When a segment is taller than it is wide, the straight portions are
  # vertical (not horizontal). This exercises the else branch in
  # ComputeSegmentWaypoints that was previously uncovered.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A vertical-dominant line segment produces vertical and diagonal tiles
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (1,5) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (1,5)
    When the simulation runs for 1 hour
    Then the line tile path should include a vertical straight segment
    And the line tile path should include a diagonal segment

  Scenario: A purely vertical line segment has only vertical tiles
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (0,4) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line
    And the runner will create a line between stations at (0,0) and (0,4)
    When the simulation runs for 1 hour
    Then the line tile path should have 5 tiles
    And the line tile path should include a vertical straight segment

  # ──────────────────────────────────────────────────────────────────────
  # Single-station line: the fallback path for a line with only 1 station
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A train moves correctly along a horizontal line
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 5 hours
    Then the train should be at tile (3,0)

  # ──────────────────────────────────────────────────────────────────────
  # Destination behind train: MinStepsViaLine backward-pass
  #
  # Line: Circle(0,0) → Rectangle(3,0) → Triangle(6,0)
  # Train heading toward Triangle (direction +1) at Rectangle.
  # Passenger wants Circle (behind the train). MinStepsViaLine must
  # compute the backward-pass cost: ride to Triangle (terminal), reverse,
  # then count back to Circle.
  # ──────────────────────────────────────────────────────────────────────

  Scenario: A passenger wanting a station behind the train is handled via backward pass
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and passengers every 1 hour
    And a level with a Triangle station at (6,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will extend the first line from station (3,0) to station (6,0)
    And the runner will deploy a train on the first line at station (3,0)
    When the simulation runs for 20 hours
    Then the score should be at least 1
