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

    Scenario: Two trains on the same line both deliver passengers
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (2,0) with a spawn delay of 0 days and passengers every 6 hours
        And a weekly gift override for week 1 with resource type Train
        And the player will create a line connecting stations at (0,0) and (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then add a second vehicle to the created line at station (2,0)
        When the simulation runs for 72 hours
        Then the total score should be greater than 0
        And the snapshot should have 2 active vehicles

    Scenario: Train with two wagons has triple capacity
        Given a level with vehicle capacity 1
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 2 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Wagon
        And a weekly gift override for week 2 with resource type Wagon
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then add first wagon to the train
        And the player will then add second wagon to the train
        When the simulation runs for 100 hours
        Then the vehicle should never have more than 3 passengers onboard
        And the total score should be greater than 0

    Scenario: Train drops off multiple passengers at the same station
        Given a level with vehicle capacity 6
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the total score should be at least 3

    Scenario: Passengers accumulate when no line is connected
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        When the simulation runs for 10 hours
        Then the total score should be 0
        And the station at (0,0) should have at least 5 passengers

    Scenario: Score does not increase without delivering to correct type
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a level with a Diamond station at (9,9) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then no vehicle should carry a Diamond passenger

    Scenario: Train picks up passengers from both directions during ping-pong
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days and passengers every 6 hours
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 72 hours
        Then the total score should be at least 2

    Scenario: Multiple trains on a three-station line deliver efficiently
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Diamond station at (2,0) with a spawn delay of 0 days and passengers every 4 hours
        And a weekly gift override for week 1 with resource type Train
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then extend the created line from station (1,0) to station (2,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then add a second vehicle to the created line at station (2,0)
        When the simulation runs for 120 hours
        Then the total score should be at least 3

    Scenario: Removing a vehicle while carrying passengers loses them
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (3,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (3,0)
        And the player will then add a vehicle to the created line at station (0,0)
        And the player will then remove the added vehicle
        When the simulation runs for 24 hours
        Then the snapshot should have 0 active vehicles
        And the snapshot should have 1 available vehicle

    Scenario: Train does not exceed capacity even with many passengers waiting
        Given a level with vehicle capacity 2
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 1 hour
        And a level with a Triangle station at (5,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (5,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the vehicle should never have more than 2 passengers onboard

    Scenario: Passengers at disconnected station are not picked up by trains on other lines
        Given a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days and passengers every 6 hours
        And a level with a Diamond station at (9,9) with a spawn delay of 0 days and passengers every 6 hours
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 48 hours
        Then the station at (9,9) should have at least 1 passengers

    Scenario: Active train prevents game over by picking up passengers
        Given a level with vehicle capacity 6
        And a level with a Circle station at (0,0) with a spawn delay of 0 days and passengers every 4 hours
        And a level with a Triangle station at (1,0) with a spawn delay of 0 days
        And a weekly gift override for week 1 with resource type Line
        And the player will create a line connecting stations at (0,0) and (1,0)
        And the player will then add a vehicle to the created line at station (0,0)
        When the simulation runs for 100 hours
        Then "OnGameOver" should have fired exactly 0 times
        And the total score should be greater than 0
