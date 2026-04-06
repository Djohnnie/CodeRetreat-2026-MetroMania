Feature: Remove Vehicle
  Tests the RemoveVehicle player action which allows a player to remove a train
  from its line and return the resource to the available pool.

  When a train has passengers on board, removal is deferred: the train enters a
  "pending removal" state where it continues moving and dropping off passengers
  at the next station(s) but does NOT pick up new ones. Once all passengers have
  been dropped off the train is physically removed and OnVehicleRemoved fires.

  Passengers dropped at a station that does not match their destination type are
  re-queued at that station (no points awarded) and wait for another train.

  # ═══════════════════════════════════════════════════════════════════════════
  #  Immediate removal (no passengers)
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Remove an empty train immediately
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first train
    When the simulation runs for 3 hours
    Then there should be 0 trains in the simulation
    And there should be 0 in-use Train resources

  Scenario: OnVehicleRemoved fires for immediate removal
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    When the simulation runs for 4 hours
    Then "OnVehicleRemoved" should have fired exactly 1 times

  Scenario: Resource becomes available after immediate removal
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will attempt to remove the first train
    And the runner will redeploy the released train on the first line at station (3,0)
    When the simulation runs for 4 hours
    Then there should be 1 train in the simulation
    And there should be 1 in-use Train resources

  # ═══════════════════════════════════════════════════════════════════════════
  #  Pending removal (train has passengers)
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Train with passengers enters pending removal
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    When the simulation runs for 4 hours
    Then there should be 1 train in the simulation
    And the first train should have pending removal

  Scenario: Pending-removal train does not pick up new passengers
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (6,0) with a spawn delay of 0 days and passengers every 24 hours
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (6,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    When the simulation runs for 20 hours
    Then the score should be 1

  Scenario: Pending-removal train drops off passengers and is then removed
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    When the simulation runs for 10 hours
    Then there should be 0 trains in the simulation
    And there should be 0 in-use Train resources
    And "OnVehicleRemoved" should have fired exactly 1 times
    And the score should be 1

  Scenario: Passenger dropped at wrong station during pending removal does not score
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Circle station at (2,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (2,0)
    And the runner will extend the first line from station (2,0) to station (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    When the simulation runs for 15 hours
    Then the score should be 0
    And there should be 0 trains in the simulation
    And there should be 1 passenger waiting at station (2,0)

  # ═══════════════════════════════════════════════════════════════════════════
  #  Error cases
  # ═══════════════════════════════════════════════════════════════════════════

  Scenario: Removing a non-existent train returns error 300
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to remove a non-existent train
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 300

  Scenario: Removing a train that is already pending removal returns error 301
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
    And a level with a Rectangle station at (5,0) with a spawn delay of 0 days and no passenger spawn phases
    And the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (5,0)
    And the runner will deploy a train on the first line at station (0,0)
    And the runner will do nothing on the next tick
    And the runner will attempt to remove the first train
    And the runner will attempt to remove the first train
    When the simulation runs for 5 hours
    Then OnInvalidPlayerAction should have fired with code 301
