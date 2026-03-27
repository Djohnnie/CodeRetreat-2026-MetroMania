Feature: Player Actions
    Players manage metro lines and vehicles through actions returned from OnHourTick.
    The engine processes each action and updates the game state accordingly.

    Background:
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line

    Scenario: Creating a line consumes a line resource and adds the line to the game state
        Given the player will create a line connecting stations at (0,0) and (1,0)
        When the simulation runs for 3 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (0,0) and (1,0) in order
        And the snapshot should have 1 available line

    Scenario: Removing a line releases the line resource
        Given the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then remove the created line
        When the simulation runs for 4 hours
        Then the snapshot should have 0 active lines
        And the snapshot should have 2 available lines

    Scenario: Adding a vehicle to a line consumes a vehicle resource
        Given the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 4 hours
        Then the snapshot should have 1 active vehicle
        And the active vehicle should be on the created line at station (0,0)
        And the snapshot should have 0 available vehicles

    Scenario: Removing a vehicle releases the vehicle resource
        Given the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then remove the added vehicle
        When the simulation runs for 5 hours
        Then the snapshot should have 0 active vehicles
        And the snapshot should have 1 available vehicle

    Scenario: Removing a line also releases its vehicles
        Given the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then remove the created line
        When the simulation runs for 5 hours
        Then the snapshot should have 0 active lines
        And the snapshot should have 0 active vehicles
        And the snapshot should have 2 available lines
        And the snapshot should have 1 available vehicle

    Scenario: Extending a line from the back adds a station
        Given the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        When the simulation runs for 4 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (0,0), (1,0) and (2,0) in order

    Scenario: Inserting a station between two existing stations
        Given the player will create a line connecting stations at (0,0) and (2,0)
        And the player will then insert station (1,0) between stations (0,0) and (2,0) on the created line
        When the simulation runs for 4 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (0,0), (1,0) and (2,0) in order
