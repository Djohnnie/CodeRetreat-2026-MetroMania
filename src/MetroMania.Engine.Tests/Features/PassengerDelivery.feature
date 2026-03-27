Feature: Passenger Pickup and Delivery
    Trains at stations pick up passengers they can deliver and drop off passengers
    whose destination matches the station type. Each passenger action takes 1 hour.
    The train stays at the station (dwells) for the total number of actions.
    Score increases by 1 for each passenger delivered.

    Scenario: Train delivers a passenger and increases the score
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the total score should be greater than 0

    Scenario: Train picks up a passenger at a station and carries it
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (3,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 5 hours
        Then the vehicle should have at least 1 passenger onboard

    Scenario: Train drops off passengers at the correct destination
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the total score should be at least 1
        And the Circle station at (0,0) should have fewer than 20 passengers

    Scenario: Train does not pick up passengers it cannot deliver
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 24 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (9,9) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then passengers with Diamond destination should still be at station (0,0)

    Scenario: Train capacity limits the number of passengers picked up
        Given a level with vehicle capacity 1
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the vehicle should never have more than 1 passenger onboard

    Scenario: Wagon capacity increases the number of passengers a train can carry
        Given a level with vehicle capacity 1
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Wagon
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then add a wagon to the train
        When the simulation runs for 48 hours
        Then the vehicle should never have more than 2 passengers onboard

    Scenario: Each passenger pickup and dropoff takes one hour of dwell time
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the vehicle should have experienced dwell time
