# DBF2SQL
This program helps to convert old dbf database into sql database

## Usage
* Put all your dbf files into the DBF folder created by the program
* Set the host to your database, port and username password with the database name
* SQL Lines will be setting how much sql lines will be generated before sending to sql server, the number larger will use more performance but faster load speed
* Encoding will needed to be set if the dbf contains special characters like chinese text. Currently only supports chinese
* Import deleted will import those lines which had been deleted
* Convert to File will try to convert the sql into file, which means we dont need the settings above, but just need the encoding
* Convert to Database will try to directly put the data into the database, which will be more faster

## Used third-party extensions
* [DbfDataReader](https://github.com/yellowfeather/DbfDataReader)
* [MySqlData](https://www.nuget.org/packages/MySql.Data/)

# About DbfDataReader
* I had modified some codes to let the system run more smoothly, however the author of the extension is not me! Please review the real author above!