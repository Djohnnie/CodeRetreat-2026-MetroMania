Feature: Remove Line
  Tests the RemoveLine player action which allows a player to remove a line
  and return the line resource to the available pool.

  When RemoveLine is performed, the line is marked with a pending removal state.
  All trains on that line are also marked for pending removal (same mechanism as
  RemoveVehicle). Trains with passengers continue moving and dropping off
  passengers but do NOT pick up new ones. Once every train has been removed, the
  line itself is physically removed and OnLineRemoved fires. The line resource
  becomes available for redeployment.

  # ═══════════════════════════════════════════════════════════════════════════
  #  Immediate removal (no trains on the line)
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Remove an empty line with no trains
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 2 hours
    Then there should be 0 lines in the simulation
    And there should be 0 in-use Line resources
    And "OnLineRemoved" should have fired exactly 1 times

  Scenario: Line resource is available for reuse after removal
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove the first line
    And the runner will recreate a line between stations at (0,0) and (3,0)
    When the simulation runs for 3 hours
    Then there should be 1 line in the simulation
    And there should be 1 in-use Line resources

  # ═══════════════════════════════════════════════════════════════════════════
  #  Removal with empty trains (no passengers)
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Remove line with an empty train removes both train and line
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 3 hours
    Then there should be 0 trains in the simulation
    And there should be 0 lines in the simulation
    And there should be 0 in-use Train resources
    And there should be 0 in-use Line resources

  Scenario: OnVehicleRemoved fires before OnLineRemoved
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first line
    When the simulation runs for 3 hours
    Then "OnVehicleRemoved" should have fired exactly 1 times
    And "OnLineRemoved" should have fired exactly 1 times
    And "OnVehicleRemoved" should have fired before "OnLineRemoved"

  # ═══════════════════════════════════════════════════════════════════════════
  #  Removal with loaded trains (passengers on board)
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Remove line with a loaded train waits for passenger drop-off
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first line
    When the simulation runs for 4 hours
    Then there should be 1 train in the simulation
    And the first train should have pending removal
    And the first line should have pending removal

  Scenario: Loaded train drops passengers then line is fully removed
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first line
    When the simulation runs for 10 hours
    Then there should be 0 trains in the simulation
    And there should be 0 lines in the simulation
    And there should be 0 in-use Train resources
    And there should be 0 in-use Line resources
    And "OnVehicleRemoved" should have fired exactly 1 times
    And "OnLineRemoved" should have fired exactly 1 times
    And the score should be 1

  # ═══════════════════════════════════════════════════════════════════════════
  #  Interaction with RemoveVehicle
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Train already pending removal is not double-flagged by RemoveLine
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    And the runner will attempt to remove the first line
    When the simulation runs for 10 hours
    Then there should be 0 trains in the simulation
    And there should be 0 lines in the simulation
    And "OnVehicleRemoved" should have fired exactly 1 times
    And "OnLineRemoved" should have fired exactly 1 times

  # ═══════════════════════════════════════════════════════════════════════════
  #  Error cases
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Removing a non-existent line returns error 400
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove a non-existent line
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 400

  Scenario: Removing a line that is already pending removal returns error 401
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first line
    And the runner will attempt to remove the first line
    When the simulation runs for 5 hours
    Then OnInvalidPlayerAction should have fired with code 401
