# Shortest-route-finder
This program uses a GoogleAPI to request the time it would take to get from one location(consumer) to several others(providers) via roads. The program will calculate the shortest time between each one and output a new file containing the information of the closest provider. The location of each place should be given by the latitude and longitude.
This program reads from 2 .csv files labelled as DataLocations and Provider and is designed for a relatively large number of items. The program efficiency is dependant on the response time from the HTTP request and the size of both tables (O(nm)).
Important to note about this program is that it requires your own key for the API and if the number of requests are over the limit of free use 
then extra requests are paid for (check https://developers.google.com/maps/documentation/javascript/get-api-key).

Sample data has been given with 1227 School locations and 14 providers. If the program runs and does not give a valid provider it may be because some locations are overseas.

For anyone attempting to adapt this solution for their own needs you will need to update a few of the global variables as their is no UI component to this program. If you encounter any difficulties or have any suggestions about this program please do not hesitate to message me.

