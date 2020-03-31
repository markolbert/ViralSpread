# ViralSpread

This is a quick-and-dirty viral disease propagation command line simulator I threw together. Not being a virologist or an epidemiologist 
I don't vouch for its accuracy. It's intended as a simple exploratory tool; use at your own risk.

## Operation

Simulator parameters can be entered via the command line (defaults get used for anything you don't define). The options are (defaults
noted in parantheses):

|Option            |Description                   |Default Value|
|------------------|------------------------------|:-----------:|
|-p, --population  |Number of people to simulate  |       1,000 |
|-n, --neighbors   |Maximum size of a neighborhood|          40 |
|-c, --contagious  |Days contagious               |          10 |
|-i, --interactions|Interactions per day          |          10 |
|-t, --transmission|Chance of transmitting virus per contact (decimal %; 0.1 = 10%)|0.05 |
|-m, --mortality   |Mortality rate (decimal %; 0.01 = 1%)| 0.01 |
|-d, --days        |Days to simulate              |          60 |
|-s, --simulations |Simulations to run            |          10 |  

The output is self-explanatory. You'll be given the chance to save the results to an xlsx Excel file.

## Concept

The simulation randomly asisgns the population to neighborhoods. The neighborhoods are sized randomly, up to approximately
the specifid maximum size.

An actual neighborhood may be larger than the specified maximum because all reciprocal relationships are included
in a neighborhood. For example, if a person's neighborhood is initialized and then a person later in the initiailization 
sequence taps that first individual as a neighbor, the later person will be added to the former person's neighborhood, pushing
it past the specified maximum.

The simulation starts by infecting one person, at random, on day 0. Potential transmission occurs with each interaction
between a person and someone in their neighborhood. Transmission between neighborhoods only occurs when a person is part of
more than one neighborhood.

A person is contagious for a limited period of time. The simulation assumes an infected person is equally contagious 
throughout the entire period of infection...which is not the case in the real world. The simulation also assumes the 
course of the disease and the period of being contagious are the same...which is also unrealistic.

During each day of being contagious an infected person faces the risk of dying. The chance of dying is spread equally over
the period of infection/contagion, so if the virus is assumed to kill 1% of the population and the disease is assumed to last
10 days, each day a person is infected they face a 0.1% chance of dying. People who die are no longer sources of potential
infection to their neighbors.
