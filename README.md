# Serial Storage Container
 An easily serializable container for storing a variety of primitive data types (or lists of them) with a name, description, and value. In portable (serialized, as a string) format, the data is stored in a simple format, with no nested data structures. 


#
 Adding Data To A SSC

The SSC supports 3 data types: **string**, **numeric** (numbers stored as the 'double' datatype) and 
**boolean**, and supports **lists** of those items. To add an item (or list of item), call the "add entry" 
function of the relevant type. Note that each "add entry" function returns a boolean (true if adding setting succeeded, false if not) and an error 
message (with the "out" keyword) through the second parameter.

Example of adding an entry with a single item (from a SSC object called testContainer):
```
// Create a string entry

if (!testContainer.addStringEntry(
     new SerialStorageContainer.sscStringEntry("String Entry #1", "First string entry", "HelloWorld"),
     out errorMessage))
{
	// display error message if adding string entry failed
        Console.WriteLine(errorMessage);
}
```

Example of adding an entry with multiple items (also known as a list) (from a SSC object called testContainer):
```
// Create a numeric list entry

if (!testContainer.addNumericEntry(
    new SerialStorageContainer.sscNumericEntry("Numeric List #1", "First numeric list entry",
    new double[3] { 3.14, 1.59, 2.65 }),
    out errorMessage))
{
	// display error message if adding entry failed
        Console.WriteLine(errorMessage);
}
```

# Getting data from a SSC

Functions exist for each supported data type to get the entry instance (such as sscStringEntry for a string entry), description, or value(s) (a list of values
from the entry instance) by the entry's name.

Example of getting an entry instance (from a SSC object called testContainer):
**NOTE: The entry instance functions return a nullable object. This object will be null if the entry cannot be found in the SSC.**
```
// Get a string entry instance from the SSC for the entry called "String Entry #1".

SerialStorageContainer.sscStringEntry? stringEntryInstance =
	testContainer.getStringEntry("String Entry #1").Value;

```

Example of getting a description from a boolean setting (from a SSC object called testContainer):
```
// Get description of a boolean entry called "Boolean Entry #1".

 string booleanEntryDescription = testContainer.getBooleanEntryDescription("Boolean Entry #1");

```

Example of getting entry values from a numeric setting (from a SSC object called testContainer):
**NOTE: The value functions will always return a list, even if the entry instance only contains a single value.**
```
// Get entry data from a numeric entry called "Numeric List #1".

List<double> numericListData = testContainer.getNumericEntryValues("Numeric List #1");

```

