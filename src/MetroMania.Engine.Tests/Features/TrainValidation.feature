Feature: Train Validation
  Verifies that the engine correctly rejects invalid AddVehicleToLine player
  actions with the appropriate error codes. Error codes 204 (line at capacity)
  and 205 (tile occupied) are covered in TrainCollision.feature.

  Background:
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Triangle station at (6,0) with a spawn delay of 0 days and no passenger spawn phases

  Scenario: Deploying a train with a non-existent resource triggers error 200
    Given the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to deploy a train with a non-existent resource on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 200

  Scenario: Deploying a train on a non-existent line triggers error 201
    Given the level has 1 initial Line and 1 initial Train
    And the runner will attempt to deploy a train on a non-existent line at station (0,0)
    When the simulation runs for 1 hour
    Then OnInvalidPlayerAction should have fired with code 201

  Scenario: Deploying a train at a station not on the line triggers error 202
    Given the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to deploy a train at station (6,0) which is not on the first line
    When the simulation runs for 2 hours
    Then OnInvalidPlayerAction should have fired with code 202

  Scenario: An invalid train deployment does not add a train
    Given the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will attempt to deploy a train with a non-existent resource on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then there should be 0 trains in the simulation
