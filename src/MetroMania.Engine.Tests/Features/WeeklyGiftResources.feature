Feature: Weekly Gift Resources
    Weekly gifts spawn new resources that become available to the player.
    The game starts with 1 Line and 1 Train resource. Each weekly gift adds
    another resource with a unique Id. All resources start as available (not InUse)
    until the player assigns them via player actions.

    Scenario: Game starts with one Line and one Train as available resources
        Given an empty level with seed 42
        When the simulation runs for 1 hour
        Then the snapshot should contain at least 1 available resource of type Line
        And the snapshot should contain at least 1 available resource of type Train

    Scenario: Random weekly gifts are added as available resources
        Given an empty level with seed 42
        When the simulation runs for 700 hours
        Then the total resource count should be the initial 2 plus the number of weekly gifts received
        And all resources in the snapshot should be available
        And all resources in the snapshot should have unique Ids

    Scenario: Overridden weekly gifts are added as correctly typed available resources
        Given an empty level with seed 42
        And a weekly gift override for week 1 with resource type Train
        And a weekly gift override for week 2 with resource type Wagon
        And a weekly gift override for week 3 with resource type Line
        When the simulation runs for 700 hours
        Then the total resource count should be the initial 2 plus the number of weekly gifts received
        And all resources in the snapshot should be available
        And the snapshot should contain at least 2 available resources of type Train
        And the snapshot should contain at least 2 available resources of type Line
        And the snapshot should contain at least 1 available resource of type Wagon

    Scenario: Each weekly gift is immediately visible as a resource in the snapshot when received
        Given an empty level with seed 42
        When the simulation runs for 700 hours
        Then each weekly gift snapshot should contain the gifted resource as available
        And the resource count should grow by 1 with each weekly gift
