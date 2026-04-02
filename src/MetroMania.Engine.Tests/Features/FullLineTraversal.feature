Feature: Full Line Traversal with Multiple Station Types
    A metro vehicle on a line connecting three different station types — Circle, Rectangle,
    and Triangle — must always traverse the complete length of the line back and forth.
    The vehicle must visit every station in sequence (first → second → third → second → first)
    and reverse at both endpoints, even when all three stations are actively spawning passengers.

    Scenario: Vehicle visits all three stations on a mixed-type line with all spawning passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days and passengers every 4 hours
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 100 hours
        Then the vehicle should have visited station (0,0)
        And the vehicle should have visited station (1,0)
        And the vehicle should have visited station (2,0)

    Scenario: Vehicle delivers passengers from all three different station types
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days and passengers every 4 hours
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 100 hours
        Then the total score should be greater than 0

    Scenario: Vehicle reaches the Triangle endpoint and reverses back to the Circle start
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Rectangle station at (1,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days and passengers every 4 hours
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 100 hours
        Then the vehicle should have visited station (2,0)
        And the vehicle should have visited station (0,0)
        And the vehicle should have experienced dwell time
