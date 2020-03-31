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
