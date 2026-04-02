# MetroMania Engine Rules

## Stations

- Metro stations are located on tiles on the map.
- Passengers spawn at a station only if there is at least one other station of a different type present on the map (the destination does not need to be connected by a line yet).

## Lines

- Metro lines connect stations to each other.
- A line can never form a loop: a station may not appear more than once on the same line, even through other stations.
- Lines are always drawn as straight horizontal or vertical segments between stations. If a direct horizontal or vertical connection is not possible, a single 45-degree diagonal segment is inserted between the horizontal or vertical parts to link the two stations.

## Distance

- Stations and lines are positioned on tiles. The distance between two stations equals the number of tile movements required to travel from one station to the other.
- Example: if there are 2 empty tiles between two stations (station → empty → empty → station), the distance is **3** tile movements.
- Distance is always a whole number (integer). Trains move 1 tile per hour.

## Train Movement

- Trains traverse a metro line from the first station to the last station, then turn around and travel back.
- Trains **always** travel the full length of the line before turning around. A train never turns around at an intermediate station.
- Trains only reverse direction when they reach either terminal (the first or last station of the line).

## Passenger Pickup

- Trains pick up passengers in **FIFO** order (first spawned, first picked up).
- A train skips a passenger if:
  - The passenger's destination is not reachable via any connected line.
  - The fastest route to the passenger's destination is by traveling in the **opposite** direction, or via a **different line** that also stops at this station.
- A train will not pick up a passenger if it is already at **full capacity**.

## Passenger Drop-off

- Trains drop off passengers before picking up new ones at the same station stop.
- Each passenger picked up or dropped off takes **1 hour**. During this time the train remains stationary at the station.
