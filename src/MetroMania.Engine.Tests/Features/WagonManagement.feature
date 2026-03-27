Feature: Wagon Management
    Wagons are attached to trains to increase passenger capacity.
    They can be added from available resources or moved between trains.

    Background:
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Wagon
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)

    Scenario: Adding a wagon to a train consumes the wagon resource
        Given the player will then add a wagon to the train
        When the simulation runs for 5 hours
        Then the snapshot should have 0 available wagons
        And the train should have 1 wagon attached

    Scenario: Adding a wagon to a non-existent train is ignored
        Given the player will then add a wagon to a random train id
        When the simulation runs for 5 hours
        Then the snapshot should have 1 available wagon
        And the train should have 0 wagons attached

    Scenario: Adding an already-used wagon is ignored
        Given the player will then add a wagon to the train
        And the player will then add the same wagon to the train again
        When the simulation runs for 6 hours
        Then the train should have 1 wagon attached

    Scenario: Removing a train also releases its wagons
        Given the player will then add a wagon to the train
        And the player will then remove the added vehicle
        When the simulation runs for 6 hours
        Then the snapshot should have 0 active vehicles
        And the snapshot should have 1 available wagon

    Scenario: Removing a line releases trains and their wagons
        Given the player will then add a wagon to the train
        And the player will then remove the created line
        When the simulation runs for 6 hours
        Then the snapshot should have 0 active vehicles
        And the snapshot should have 1 available wagon

    Scenario: Adding a wagon directly to a line via AddVehicleToLine is ignored
        Given the player will then add a wagon directly to the line at station (0,0)
        When the simulation runs for 5 hours
        Then the snapshot should have 1 available wagon
        And the snapshot should have 1 active vehicle

    Scenario: Moving a wagon between two trains
        Given a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Train
        And the player will create a second line connecting stations at (1,0) and (2,0)
        And the player will then add a vehicle to the second line at station (1,0)
        And the player will then add a wagon to the first train
        And the player will then move the wagon from the first train to the second train
        When the simulation runs for 400 hours
        Then the first train should have 0 wagons attached
        And the second train should have 1 wagon attached

    Scenario: Moving a wagon from a train that does not own it is ignored
        Given a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 2 with resource type Line
        And a weekly gift override for week 3 with resource type Train
        And the player will create a second line connecting stations at (1,0) and (2,0)
        And the player will then add a vehicle to the second line at station (1,0)
        And the player will then add a wagon to the first train
        And the player will then move the wagon from the second train to the first train
        When the simulation runs for 400 hours
        Then the first train should have 1 wagon attached
        And the second train should have 0 wagons attached

    Scenario: Wagon snapshot exposes navigation to its parent train
        Given the player will then add a wagon to the train
        When the simulation runs for 5 hours
        Then the wagon should reference the train via navigation
        And the train should reference the wagon via navigation

    Scenario: Adding multiple wagons to a single train increases capacity proportionally
        Given a weekly gift override for week 2 with resource type Wagon
        And the player will then add a wagon to the train
        And the player will then add second wagon to the train
        When the simulation runs for 400 hours
        Then the train should have 2 wagons attached

    Scenario: Moving a wagon to the same train it is already on is ignored
        Given the player will then add a wagon to the train
        And the player will then move the wagon from the first train to the first train
        When the simulation runs for 400 hours
        Then the first train should have 1 wagon attached
