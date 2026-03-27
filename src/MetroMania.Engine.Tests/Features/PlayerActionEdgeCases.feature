Feature: Player Action Edge Cases
    Edge cases and invalid action handling for player actions.
    Invalid actions should be silently ignored without changing game state.

    Scenario: Creating a line with only one station is ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will attempt to create a line with only station (0,0)
        When the simulation runs for 3 hours
        Then the snapshot should have 0 active lines
        And the snapshot should have 2 available lines

    Scenario: Extending a line from the front adds a station at position 0
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (0,0) to station (2,0)
        When the simulation runs for 4 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (2,0), (0,0) and (1,0) in order

    Scenario: Extending a line from a middle station is ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a level with a Star station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then extend the created line from station (1,0) to station (3,0)
        When the simulation runs for 5 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (0,0), (1,0) and (2,0) in order

    Scenario: Inserting a station between non-adjacent stations is ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a level with a Rectangle station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then insert station (3,0) between stations (0,0) and (2,0) on the created line
        When the simulation runs for 5 hours
        Then the snapshot should have 1 active line
        And the active line should connect stations (0,0), (1,0) and (2,0) in order

    Scenario: Adding a vehicle at a station not on the line is ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (2,0)
        When the simulation runs for 4 hours
        Then the snapshot should have 0 active vehicles
        And the snapshot should have 1 available vehicle

    Scenario: Removing a non-existent line is silently ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will attempt to remove a line with a random id
        When the simulation runs for 3 hours
        Then the snapshot should have 0 active lines
        And the snapshot should have 2 available lines

    Scenario: Creating a line when no line resources are available is ignored
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will create a line connecting stations at (1,0) and (2,0)
        And the player will create a line connecting stations at (0,0) and (2,0)
        When the simulation runs for 5 hours
        Then the snapshot should have 2 active lines
        And the snapshot should have 0 available lines
