Feature: Resource Lifecycle
  Verifies that resources correctly track their InUse status as lines and trains
  are created and deployed, and that each resource has a unique identifier.

  Background:
    Given a level with a Circle station at (0,0) with a spawn delay of 0 days and no passenger spawn phases
    And a level with a Rectangle station at (3,0) with a spawn delay of 0 days and no passenger spawn phases

  Scenario: Initial resources start as not in use
    Given the level has 2 initial Lines and 2 initial Trains
    When the simulation runs for 1 hour
    Then all initial resources should not be in use

  Scenario: Creating a line marks the Line resource as in-use
    Given the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    When the simulation runs for 1 hour
    Then there should be 1 in-use Line resource
    And there should be 0 unused Line resources

  Scenario: Deploying a train marks the Train resource as in-use
    Given the level has 1 initial Line and 1 initial Train
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then there should be 1 in-use Train resource
    And there should be 0 unused Train resources

  Scenario: Unused resources remain after some are consumed
    Given the level has 2 initial Lines and 2 initial Trains
    And the runner will create a line between stations at (0,0) and (3,0)
    And the runner will deploy a train on the first line at station (0,0)
    When the simulation runs for 2 hours
    Then there should be 1 unused Line resource
    And there should be 1 unused Train resource

  Scenario: All resources have unique IDs
    Given the level has 2 initial Lines and 2 initial Trains
    When the simulation runs for 1 hour
    Then all resources should have unique IDs
