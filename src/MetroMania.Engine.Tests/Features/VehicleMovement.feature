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
