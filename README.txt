Raffinose experiments
---------------------
Data for a budding yeast BY strain growing in different concentrations of raffinose in a Tecan plate reader after pregrowth in 2% pyruvate:

raffinose_r.tsv    raw time-series data directly from the plate reader
raffinose_s.tsv    processed time-series data that combines data from replicates
raffinose_sc.tsv   summary statistics

Fig 1B shows data from raffinose_s.tsv.

HXT experiments
---------------
Data for budding yeast BY strains - wild-type and strains deleted for one of the SNF3, RGT2, MTH1, and STD1 genes -- growing in 0.2% and 2% glucose in a Tecan plate reader after pregrowth in 2% pyruvate:

HXTdeletions_r.tsv    raw time-series data directly from the plate reader
HXTdeletions_s.tsv    processed time-series data that combines data from replicates
HXTdeletions_sc.tsv   summary statistics

Fig 2B-G shows data from HXTdeletions_s.tsv.

Analysing with omniplate
------------------------
The three files for each set of experiments can be imported directly into omniplate using Python.
For example,

>>> import omniplate as om
>>> p.importdf('raffinose')
