Feature: Vehicle Movement
    Vehicles move along their lines at 1 grid unit per hour.
    Distance between stations is Euclidean (straight line).
    Vehicles ping-pong: they reverse direction when reaching either endpoint.
    Vehicles move BEFORE the player acts each tick, so newly placed vehicles
    start moving on the next tick.

    Scenario: Vehicle moves at the correct speed along a segment
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (4,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (4,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 4 hours
        Then the vehicle should have segment index 0 with progress 0.5 and direction 1
        And the vehicle should not be at a station

    Scenario: Vehicle completes a full ping-pong cycle
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 6 hours
        Then the vehicle should be at station (0,0) with direction 1

    Scenario: Vehicle traverses multiple segments
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 5 hours
        Then the vehicle should be at station (2,0) with direction -1

    Scenario: Vehicle placed at last station starts moving backward
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (2,0)
        And the player will then add a vehicle to the created line at station (2,0)
        When the simulation runs for 4 hours
        Then the vehicle should be at station (0,0) with direction 1

    Scenario: Vehicle moves correctly on a diagonal segment
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (3,4) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (3,4)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 7 hours
        Then the vehicle should be at station (3,4) with direction -1

    Scenario: Two vehicles on the same line move independently
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Train
        And the player will create a line connecting stations at (0,0) and (3,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then add a second vehicle to the created line at station (3,0)
        When the simulation runs for 5 hours
        Then the snapshot should have 2 active vehicles
        And vehicle 0 should be at station (3,0) with direction -1

    Scenario: Vehicle dwells at station during passenger loading
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the total score should be greater than 0
        And the vehicle should have experienced dwell time

    Scenario: Vehicle resumes movement after dwell completes
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the total score should be at least 1

    Scenario: Vehicle on a long multi-segment line reaches the end and returns
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days
        And a level with a Rectangle station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then extend the created line from station (2,0) to station (3,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 7 hours
        Then the vehicle should be at station (3,0) with direction -1

    Scenario: Vehicle carries over remaining speed across segment boundaries
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (1,1) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (1,1)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 5 hours
        Then the vehicle should be at station (1,1) with direction -1

    Scenario: Newly placed vehicle does not move on its placement tick
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 2 hours
        Then the vehicle should be at station (0,0) with direction 1
