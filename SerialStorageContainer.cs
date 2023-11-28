using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;

namespace SerialStorageContainer
{
    /// <summary>
    /// A container for storing a variety of primitive data types (or lists of them) with a name,
    /// description, and value. In portable (serialized, as a string) format, the data is stored in a simple, serial format, with no
    /// nested data structures.
    /// </summary>
    public class SerialStorageContainer
    {
        // constants
        public const string STRING_SETTING_IDENTIFIER = "STRING";
        public const string NUMERIC_SETTING_IDENTIFIER = "NUMERIC";
        public const string BOOLEAN_SETTING_IDENTIFIER = "BOOLEAN";
        public const string LIST_IDENTIFIER = "LIST";
        public const string LIST_END_IDENTIFIER = "##END##";

        // represents the datatype of an entry's value
        public enum EntryType
        {
            String, Numeric, Boolean, UNDEFINED
        }

        // represents the stage that the SSC file parser is in
        public enum ParserStage
        {
            // stage parser is in at start of program or after an item has been fully parsed (end of list reached,
            // after item parsed, etc)
            SearchingForItem, 
            // parser is in this stage only after it finds a list identifier.
            SearchingForDatatype,
            // stage parser is in after it has found the item's datatype
            SearchingForName, 
            // stage parser is in after it has found the item's name
            SearchingForDescription,
            // stage parser is in after it has found item's description and datatype and it is not searching for a list
            SearchingForValue,
            // stage parser is in after it has found an item's name and datatype, and is searching for the end of a list
            ParsingList
        }

        // Data structure to store string entries
        public struct sscStringEntry
        {
            // fields
            // -name of entry
            private string entryName;
            // -description of entry
            private string entryDescription;
            // -value(s) of entry
            private List<string> entryValues;

            // getters
            public string Name { get { return entryName; } }
            public string Description { get { return entryDescription; } }
            public ReadOnlyCollection<string> Values 
            { 
                get 
                {
                    if (entryValues != null)
                    {
                        return entryValues.AsReadOnly();
                    }
                    else
                    {
                        return null;
                    }
                } 
            }

            // single item constructor
            public sscStringEntry(string entryName, string entryDescription, string entryValue)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<string>();
                this.entryValues.Add(entryValue);
            }

            // list item constructor
            public sscStringEntry(string entryName, string entryDescription, List<string> entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<string>();
                this.entryValues = entryValues;
            }

            // array item constructor
            public sscStringEntry(string entryName, string entryDescription, string[] entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<string>();
                this.entryValues = entryValues.ToList();
            }
        }

        // Data structure to store numeric entries
        public struct sscNumericEntry
        {
            // fields
            // -name of entry
            private string entryName;
            // -description of entry
            private string entryDescription;
            // -value(s) of entry
            private List<double> entryValues;

            // getters
            public string Name { get { return entryName; } }
            public string Description { get { return entryDescription; } }
            public ReadOnlyCollection<double> Values { get { return entryValues.AsReadOnly(); } }

            // single item constructor
            public sscNumericEntry(string entryName, string entryDescription, double entryValue)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<double>();
                this.entryValues.Add(entryValue);
            }

            // list item constructor
            public sscNumericEntry(string entryName, string entryDescription, List<double> entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<double>();
                this.entryValues = entryValues;
            }

            // array item constructor
            public sscNumericEntry(string entryName, string entryDescription, double[] entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<double>();
                this.entryValues = entryValues.ToList();
            }
        }

        // Data structure to store boolean entries
        public struct sscBooleanEntry
        {
            // fields
            // -name of entry
            private string entryName;
            // -description of entry
            private string entryDescription;
            // -value(s) of entry
            private List<bool> entryValues;

            // getters
            public string Name { get { return entryName; } }
            public string Description { get { return entryDescription; } }
            public ReadOnlyCollection<bool> Values { get { return entryValues.AsReadOnly(); } }

            // single item constructor
            public sscBooleanEntry(string entryName, string entryDescription, bool entryValue)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<bool>();
                this.entryValues.Add(entryValue);
            }

            // list item constructor
            public sscBooleanEntry(string entryName, string entryDescription, List<bool> entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<bool>();
                this.entryValues = entryValues;
            }

            // array item constructor
            public sscBooleanEntry(string entryName, string entryDescription, bool[] entryValues)
            {
                this.entryName = entryName;
                this.entryDescription = entryDescription;
                this.entryValues = new List<bool>();
                this.entryValues = entryValues.ToList();
            }
        }

        #region FIELDS

        // store individual entries, by name and by entry type
        private Dictionary<string, sscStringEntry> sscStringEntries;
        private Dictionary<string, sscNumericEntry> sscNumericEntries;
        private Dictionary<string, sscBooleanEntry> sscBooleanEntries;

        // list of all entry names (item1)
        // item2 represent the type of value that the entry's data is
        private List<Tuple<string, EntryType>> sscEntriesMasterList;

        // stores errors generated when trying to read a configuration file
        private string errorLog;

        // stores text from SSC file (when it is read into the program)
        // when updating SSC file, text is written back into this string
        private string serializedSSCText;

        #endregion

        #region GETTERS

        // getters
        /// <summary>
        /// A readonly list of tuples representing the entries; the first item is the entry's name, and the
        /// second item represents the entry's datatype. To populate this list, initialize the function with text from an
        /// SSC file.
        /// </summary>
        public ReadOnlyCollection<Tuple<string, EntryType>> ConfigurationEntrys
        {
            get
            {
                return sscEntriesMasterList.AsReadOnly();
            }
        }
        /// <summary>
        /// A string representing the raw SSC file text.
        /// </summary>
        public string RawConfigFileText { get { return serializedSSCText; } }

        #endregion

        #region CONSTRUCTORS

        // default constructor
        public SerialStorageContainer(Tuple<string, sscStringEntry>[] stringEntries,
            Tuple<string, sscNumericEntry>[] numericEntries,
            Tuple<string, sscBooleanEntry>[] booleanEntries)
        {
            // initialize variables
            sscStringEntries = new Dictionary<string, sscStringEntry>();
            sscNumericEntries = new Dictionary<string, sscNumericEntry>();
            sscBooleanEntries = new Dictionary<string, sscBooleanEntry>();
            sscEntriesMasterList = new List<Tuple<string, EntryType>>();
            errorLog = "";

            // populate input arrays into dictionaries
            #region POPULATE PARAMETERS INTO OBJECT VARIABLES

            // numeric-based entries
            if (numericEntries != null)
            {
                for (int m = 0; m < numericEntries.Length; m++)
                {
                    // add entry to master list
                    sscEntriesMasterList.Add(new Tuple<string, EntryType>(numericEntries[m].Item1, EntryType.Numeric));

                    // add entry to numeric entries dictionary
                    sscNumericEntries.Add(numericEntries[m].Item1, numericEntries[m].Item2);
                }
            }

            // string-based entries
            if (stringEntries != null)
            {
                for (int m = 0; m < stringEntries.Length; m++)
                {
                    // add entry to master list
                    sscEntriesMasterList.Add(new Tuple<string, EntryType>(stringEntries[m].Item1, EntryType.String));

                    // add entry to string entries dictionary
                    sscStringEntries.Add(stringEntries[m].Item1, stringEntries[m].Item2);
                }
            }

            // boolean-based entries
            if (booleanEntries != null)
            {
                for (int m = 0; m < booleanEntries.Length; m++)
                {
                    // add entry to master list
                    sscEntriesMasterList.Add(new Tuple<string, EntryType>(booleanEntries[m].Item1, EntryType.Boolean));

                    // add entry to boolean entries dictionary
                    sscBooleanEntries.Add(booleanEntries[m].Item1, booleanEntries[m].Item2);
                }
            }

            #endregion
        }

        // initialize an SSC from a string (serialized SSC)
        public SerialStorageContainer(string serializedSSCText)
        {
            // initialize variables
            sscStringEntries = new Dictionary<string, sscStringEntry>();
            sscNumericEntries = new Dictionary<string, sscNumericEntry>();
            sscBooleanEntries = new Dictionary<string, sscBooleanEntry>();
            sscEntriesMasterList = new List<Tuple<string, EntryType>>();
            errorLog = "";
            this.serializedSSCText = serializedSSCText;

            // populate entries from serialized SSC string
            loadSSCFromString(serializedSSCText);
        }

        #endregion

        #region I/O FUNCTIONS

        /// <summary>
        /// Loads entries from a string containing a serialized SSC, passed as the parameter "serializedSSCString".
        /// </summary>
        /// <param name="serializedSSCString">The text of the configuration file to read.</param>
        private void loadSSCFromString(string serializedSSCString)
        {
            // ensure string length is > 0
            if (serializedSSCString.Length > 0)
            {
                // declare local variables
                string currentTextLine = "";
                string currentItemName = "";
                string currentItemDescription = "";

                double extractedDoubleValue = 0;
                bool extractedBoolValue = false;

                List<string> extractedStringItems = new List<string>();
                List<double> extractedNumericItems = new List<double>();
                List<bool> extractedBoolItems = new List<bool>();

                bool itemIsList = false;

                ParserStage parserCurrentStage = ParserStage.SearchingForItem;
                EntryType entryDataType = EntryType.UNDEFINED;

                Dictionary<string, EntryType> parsedEntryNamesDict
                    = new Dictionary<string,EntryType>();

                List<string> fileLines = new List<string>();
                int currentLineIndex = 0;

                #region CONVERT FILE TEXT INTO LINES
                // convert text into lines of text using stringReader
                StringReader stringReader = new StringReader(serializedSSCString);
                string lastReadLine = "";

                while (lastReadLine != null)
                {
                    // read another line from stringReader
                    lastReadLine = stringReader.ReadLine();

                    if (lastReadLine != null)
                    {
                        // add to file line list
                        fileLines.Add(lastReadLine);
                    }
                }

                #endregion

                // read/parse file contents
                while (currentLineIndex < fileLines.Count)
                {
                    // populate current text line
                    currentTextLine = fileLines[currentLineIndex];

                    // pick an action based on what state/stage the parser is in
                    if (parserCurrentStage == ParserStage.SearchingForItem)
                    {
                        #region FIND DATATYPE OR LIST MARKER

                        // check if current line matches either the list marker or any datatype marker
                        if (currentTextLine == STRING_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.String;

                            // set "itemIsList" to false since item isn't a list
                            itemIsList = false;

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else if (currentTextLine == NUMERIC_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.Numeric;

                            // set "itemIsList" to false since item isn't a list
                            itemIsList = false;

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else if (currentTextLine == BOOLEAN_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.Boolean;

                            // set "itemIsList" to false since item isn't a list
                            itemIsList = false;

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else if (currentTextLine == LIST_IDENTIFIER)
                        {
                            // set "itemIsList" to true since item is a list
                            itemIsList = true;

                            // need to find datatype; move parser to that stage
                            parserCurrentStage = ParserStage.SearchingForDatatype;
                        }
                        else if (currentTextLine == "")
                        {
                            // move on, empty line detected
                        }
                        else
                        {
                            // error occurred (unexpected input)
                            errorLog += "Error on line " + (currentLineIndex + 1).ToString() + "; parser is in 'SearchingForItem', stage. Expected datatype or list identifier, found '" +
                                currentTextLine + "'. Cannot proceed." + Environment.NewLine;
                            // break since SSC is unreadable
                            break;
                        }

                        #endregion
                    }
                    else if (parserCurrentStage == ParserStage.SearchingForDatatype)
                    {
                        #region FIND LIST DATATYPE

                        // check if current line matches either the list marker or any datatype marker
                        if (currentTextLine == STRING_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.String;

                            // reset list container
                            extractedStringItems = new List<string>();

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else if (currentTextLine == NUMERIC_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.Numeric;

                            // reset list container
                            extractedNumericItems = new List<double>();

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else if (currentTextLine == BOOLEAN_SETTING_IDENTIFIER)
                        {
                            // datatype detected; set it to string type
                            entryDataType = EntryType.Boolean;

                            // reset list container
                            extractedBoolItems = new List<bool>();

                            // move parser stage to searching for item name
                            parserCurrentStage = ParserStage.SearchingForName;
                        }
                        else
                        {
                            // error occurred (unexpected input)
                            errorLog += "Error on line " + (currentLineIndex + 1).ToString() + "; parser is in 'SearchingForDataType', stage. Expected datatype of list, found '" +
                                currentTextLine + "'. Cannot proceed." + Environment.NewLine;
                            // break since serialized SSC is unreadable
                            break;
                        }

                        #endregion
                    }
                    else if (parserCurrentStage == ParserStage.SearchingForName)
                    {
                        #region FIND CONFIG SETTING NAME

                        // entry name should be on current line
                        // check if name already exists in sscEntriesMasterList
                        if (!parsedEntryNamesDict.ContainsKey(currentTextLine))
                        {
                            // doesn't exist; add to list with current parsed datatype
                            parsedEntryNamesDict.Add(currentTextLine, entryDataType);

                            // update item name
                            currentItemName = currentTextLine;

                            // move parser to next stage [find description]
                            parserCurrentStage = ParserStage.SearchingForDescription;
                        }
                        else
                        {
                            // throw error; can't have 2 entries with same name
                            errorLog += "Error on line " + (currentLineIndex + 1).ToString() + "; parser is in 'SearchingForName', stage. Duplicate entry name '" +
                                currentTextLine + "' detected. Cannot proceed." + Environment.NewLine;
                            // break since serialized SSC string is unreadable
                            break;
                        }

                        #endregion
                    }
                    else if (parserCurrentStage == ParserStage.SearchingForDescription)
                    {
                        #region FIND SETTING DESCRIPTION

                        // description should just be a string, nothing to check.
                        currentItemDescription = currentTextLine;

                        // if no description specified, substitute text "[No description specified]".
                        if (currentItemDescription == "" || currentItemDescription == null)
                        {
                            currentItemDescription = "[No description specified.]";
                        }

                        // move parser to next stage, based on if configuration is a list or not
                        if (itemIsList)
                        {
                            // set parser to "parsing list" stage, since parsing list
                            parserCurrentStage = ParserStage.ParsingList;
                        }
                        else
                        {
                            // set parser to "searching for value" stage, since trying to find item value
                            parserCurrentStage = ParserStage.SearchingForValue;
                        }

                        #endregion
                    }
                    else if (parserCurrentStage == ParserStage.SearchingForValue ||
                        parserCurrentStage == ParserStage.ParsingList)
                    {
                        // check if is list
                        if (itemIsList)
                        {
                            #region CHECK FOR END-OF-LIST MARKER

                            // check for end of list marker
                            if (currentTextLine == LIST_END_IDENTIFIER)
                            {
                                #region ADD ITEM TO CONFIGURATION SETTINGS

                                if (entryDataType == EntryType.String)
                                {
                                    // add entry to specific list
                                    sscStringEntries.Add(currentItemName, new sscStringEntry(currentItemName,
                                        currentItemDescription, extractedStringItems));
                                }
                                else if (entryDataType == EntryType.Numeric)
                                {
                                    // add entry to specific list
                                    sscNumericEntries.Add(currentItemName, new sscNumericEntry(currentItemName,
                                        currentItemDescription, extractedNumericItems));
                                }
                                else if (entryDataType == EntryType.Boolean)
                                {
                                    // add entry to specific list
                                    sscBooleanEntries.Add(currentItemName, new sscBooleanEntry(currentItemName,
                                        currentItemDescription, extractedBoolItems));
                                }

                                #endregion

                                // move parser back to "search for item" stage
                                parserCurrentStage = ParserStage.SearchingForItem;
                            }

                            // otherwise proceed

                            #endregion
                        }

                        // -if parser has changed state (after reaching end of list), don't
                        // want to continue (and treat end-of-list marker as an item to parse), so
                        // if parser is not in the 'searching for value' or 'parsing list' stage, don't
                        // proceed.
                        // -Also, if editor forgot to include an end-of-list marker and the final item is a list,
                        // currentTextLine will be null. Don't want to proceed, because that could throw an exception.
                        // Also if the line is null there is nothing to parse anyways.
                        if ((parserCurrentStage == ParserStage.SearchingForValue ||
                            parserCurrentStage == ParserStage.ParsingList) &&
                            currentTextLine != null)
                        {
                            // check if item has leading tab character (considered a best practice for readability)
                            // if yes, remove leading whitespace (to avoid incorrect formatting for strings)
                            if (currentTextLine[0] == '\t')
                            {
                                while (currentTextLine[0] == ' ' || currentTextLine[0] == '\t')
                                {
                                    // remove leading whitespace to avoid affecting string item formatting
                                    currentTextLine = currentTextLine.Remove(0, 1);

                                    // check if string is empty
                                    if (currentTextLine.Length == 0)
                                    {
                                        break;
                                    }
                                }
                            }

                            #region FIND SINGLE ITEM CONFIG SETTING VALUE

                            // check if item matches conditions (bool or numeric)
                            if (entryDataType == EntryType.String)
                            {
                                #region STRING VALUE

                                if (!itemIsList || (itemIsList && extractedStringItems.Count == 0))
                                {
                                    // no checks needed; create item
                                    // add entry to master list
                                    sscEntriesMasterList.Add(new Tuple<string, EntryType>(currentItemName, entryDataType));
                                }

                                // check if list
                                if (!itemIsList)
                                {
                                    // add entry to specific list
                                    sscStringEntries.Add(currentItemName, new sscStringEntry(currentItemName,
                                        currentItemDescription, currentTextLine));

                                    // move parser to "SearchingForItem" stage (since current item fully parsed)
                                    parserCurrentStage = ParserStage.SearchingForItem;
                                }
                                else
                                {
                                    // add item to list
                                    extractedStringItems.Add(currentTextLine);
                                    // wait for end of list
                                }

                                #endregion
                            }
                            else if (entryDataType == EntryType.Numeric)
                            {
                                #region NUMERIC VALUE

                                // check if can successfully parse a double
                                if (double.TryParse(currentTextLine, out extractedDoubleValue))
                                {
                                    // check if list
                                    if (!itemIsList || (itemIsList && extractedNumericItems.Count == 0))
                                    {
                                        // add entry to master list
                                        sscEntriesMasterList.Add(new Tuple<string, EntryType>(currentItemName, entryDataType));
                                    }

                                    // check if item is list
                                    if (!itemIsList)
                                    {
                                        // add entry to specific list
                                        sscNumericEntries.Add(currentItemName, new sscNumericEntry(currentItemName,
                                            currentItemDescription, extractedDoubleValue));

                                        // move parser to "SearchingForItem" stage (since current item fully parsed)
                                        parserCurrentStage = ParserStage.SearchingForItem;
                                    }
                                    else
                                    {
                                        // add item to list
                                        extractedNumericItems.Add(extractedDoubleValue);
                                        // wait for end of list
                                    }
                                }
                                else
                                {
                                    // throw error; invalid input
                                    errorLog += "Error on line " + (currentLineIndex + 1).ToString() + "; parser is in 'SearchingForValue', stage. Expected numeric value, could not parse text '" +
                                        currentTextLine + "' into a NUMERIC value. Cannot proceed." + Environment.NewLine;
                                    // break since serialized SSC string is unreadable
                                    break;
                                }

                                #endregion
                            }
                            else if (entryDataType == EntryType.Boolean)
                            {
                                #region BOOLEAN VALUE
                                // ensure that text in line confirms to either "True", "False" or any combination (casing-wise)
                                if (currentTextLine.ToLower() == "true" || currentTextLine.ToLower() == "false")
                                {
                                    // check if list
                                    if (!itemIsList || (itemIsList && extractedBoolItems.Count == 0))
                                    {
                                        // add entry to master list
                                        sscEntriesMasterList.Add(new Tuple<string, EntryType>(currentItemName, entryDataType));
                                    }

                                    // determine whether is true or false
                                    if (currentTextLine.ToLower() == "true")
                                    {
                                        // assign value
                                        extractedBoolValue = true;
                                    }
                                    else if (currentTextLine.ToLower() == "false")
                                    {
                                        // assign value
                                        extractedBoolValue = false;
                                    }

                                    // check if item is list
                                    if (!itemIsList)
                                    {
                                        // add entry to specific dictionary
                                        sscBooleanEntries.Add(currentItemName, new sscBooleanEntry(currentItemName,
                                            currentItemDescription, extractedBoolValue));

                                        // move parser to "SearchingForItem" stage (since current item fully parsed)
                                        parserCurrentStage = ParserStage.SearchingForItem;
                                    }
                                    else
                                    {
                                        // add item to list
                                        extractedBoolItems.Add(extractedBoolValue);
                                        // wait for end of list
                                    }
                                }
                                else
                                {
                                    // throw error; invalid input
                                    errorLog += "Error on line " + (currentLineIndex + 1).ToString() + "; parser is in 'SearchingForValue', stage. Expected boolean value, could not parse text '" +
                                        currentTextLine + "' into a BOOLEAN value. Cannot proceed." + Environment.NewLine;
                                    // break since serialized SSC string is unreadable
                                    break;
                                }

                                #endregion
                            }

                            #endregion
                        }
                    }

                    currentLineIndex++;
                }
            }
        }
        
        /// <summary>
        /// Returns a serialized copy of the SSC as a string that contains all the
        /// information stored in this SSC object.
        /// </summary>
        /// <returns>A serialized copy of the SSC that contains all the
        /// information stored in this SSC object.</returns>
        public string serializeSSC()
        {
            // declare local variables
            string serializedSSCString = "";
            bool isItemAList = false;

            #region NOTES

            /*---------PROCEDURE-------
             * 1. Iterate through each entry list (string Entries, numeric entries, boolean entries)
             * 2. Check if the current item is a list or not. If it is, add the list marker and a newline character to the serializedSSCString string.
             * 3. Put the corresponding item's datatype identifier/marker and a newline character to the serializedSSCString string.
             * 4. Put the corresponding item's name and a newline character to the serializedSSCString string.
             * 5. Put the corresponding item's description and a newline character to the serializedSSCString string.
             * 6. For each value in the item's entry struct, cast to string, append a tab character in front of the item and add with a newline character
             *      to the serializedSSCString string.
             * 7. If item is a list, at the end of the list add the end list marker and a newline charcter to the serializedSSCString string.
             * 8. Add a blank line (newline character) for spacing.
             */

            #endregion

            // format string-based entries into serialized SSC string
            for (int m = 0; m < sscStringEntries.Count; m++)
            {
                #region STRING SETTINGS

                // 1. List marker (if applicable)
                // check if item is list or not (if value count > 1)
                if (sscStringEntries.ElementAt(m).Value.Values.Count > 1)
                {
                    // is a list
                    isItemAList = true;

                    // add list marker along with newline character to output
                    serializedSSCString += LIST_IDENTIFIER + Environment.NewLine;
                }
                else
                {
                    // not a list
                    isItemAList = false;
                }

                // 2. Item datatype (type: string)
                serializedSSCString += STRING_SETTING_IDENTIFIER + Environment.NewLine;

                // 3. Item name
                serializedSSCString += sscStringEntries.ElementAt(m).Key + Environment.NewLine;

                // 4. Item description
                serializedSSCString += sscStringEntries.ElementAt(m).Value.Description + Environment.NewLine;

                // 5. Item value(s)
                for (int r = 0; r < sscStringEntries.ElementAt(m).Value.Values.Count; r++)
                {
                    // indent with tab for human readability
                    serializedSSCString += '\t' + sscStringEntries.ElementAt(m).Value.Values[r] + Environment.NewLine;
                }

                // 6. End-of-list marker (if applicable)
                if (isItemAList)
                {
                    serializedSSCString += LIST_END_IDENTIFIER + Environment.NewLine;
                }

                // 7. Blank line (for spacing/readability)
                serializedSSCString += Environment.NewLine;

                #endregion
            }

            // format numeric-based entries into serialized SSC string
            for (int m = 0; m < sscNumericEntries.Count; m++)
            {
                #region NUMERIC SETTINGS

                // 1. List marker (if applicable)
                // check if item is list or not (if value count > 1)
                if (sscNumericEntries.ElementAt(m).Value.Values.Count > 1)
                {
                    // is a list
                    isItemAList = true;

                    // add list marker along with newline character to output
                    serializedSSCString += LIST_IDENTIFIER + Environment.NewLine;
                }
                else
                {
                    // not a list
                    isItemAList = false;
                }

                // 2. Item datatype (type: numeric)
                serializedSSCString += NUMERIC_SETTING_IDENTIFIER + Environment.NewLine;

                // 3. Item name
                serializedSSCString += sscNumericEntries.ElementAt(m).Key + Environment.NewLine;

                // 4. Item description
                serializedSSCString += sscNumericEntries.ElementAt(m).Value.Description + Environment.NewLine;

                // 5. Item value(s)
                for (int r = 0; r < sscNumericEntries.ElementAt(m).Value.Values.Count; r++)
                {
                    // indent with tab for human readability
                    serializedSSCString += '\t' + sscNumericEntries.ElementAt(m).Value.Values[r].ToString() + Environment.NewLine;
                }

                // 6. End-of-list marker (if applicable)
                if (isItemAList)
                {
                    serializedSSCString += LIST_END_IDENTIFIER + Environment.NewLine;
                }

                // 7. Blank line (for spacing/readability)
                serializedSSCString += Environment.NewLine;

                #endregion
            }

            // format boolean-based entries into serialized SSC string
            for (int m = 0; m < sscBooleanEntries.Count; m++)
            {
                #region BOOLEAN SETTINGS

                // 1. List marker (if applicable)
                // check if item is list or not (if value count > 1)
                if (sscBooleanEntries.ElementAt(m).Value.Values.Count > 1)
                {
                    // is a list
                    isItemAList = true;

                    // add list marker along with newline character to output
                    serializedSSCString += LIST_IDENTIFIER + Environment.NewLine;
                }
                else
                {
                    // not a list
                    isItemAList = false;
                }

                // 2. Item datatype (type: boolean)
                serializedSSCString += BOOLEAN_SETTING_IDENTIFIER + Environment.NewLine;

                // 3. Item name
                serializedSSCString += sscBooleanEntries.ElementAt(m).Key + Environment.NewLine;

                // 4. Item description
                serializedSSCString += sscBooleanEntries.ElementAt(m).Value.Description + Environment.NewLine;

                // 5. Item value(s)
                for (int r = 0; r < sscBooleanEntries.ElementAt(m).Value.Values.Count; r++)
                {
                    // indent with tab for human readability
                    serializedSSCString += '\t' + sscBooleanEntries.ElementAt(m).Value.Values[r].ToString() + Environment.NewLine;
                }

                // 6. End-of-list marker (if applicable)
                if (isItemAList)
                {
                    serializedSSCString += LIST_END_IDENTIFIER + Environment.NewLine;
                }

                // 7. Blank line (for spacing/readability)
                serializedSSCString += Environment.NewLine;

                #endregion
            }

            return serializedSSCString;
        }

        #endregion

        #region GETTER FUNCTIONS

        #region ENTRIES

        /// <summary>
        /// Returns a copy of a string entry with the given name. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the string entry to be retrieved.</param>
        /// <returns>The string entry object if found. Returns null if entry not found.</returns>
        public sscStringEntry? getStringEntry(string entryName)
        {
            // check if entry name exists in sscStringEntries
            if (sscStringEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscStringEntries[entryName];
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns a copy of a numeric entry with the given name. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the numeric entry to be retrieved.</param>
        /// <returns>The numeric entry object if found. Returns null if entry not found.</returns>
        public sscNumericEntry? getNumericEntry(string entryName)
        {
            // check if entry name exists in sscNumericEntries
            if (sscNumericEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscNumericEntries[entryName];
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns a copy of a boolean entry with the given name. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the boolean entry to be retrieved.</param>
        /// <returns>The boolean entry object if found. Returns null if entry not found.</returns>
        public sscBooleanEntry? getBooleanEntry(string entryName)
        {
            // check if entry name exists in sscBooleanEntries
            if (sscBooleanEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscBooleanEntries[entryName];
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }


        #endregion

        #region ENTRY VALUE GETTERS

        /// <summary>
        /// Returns the value(s) of a entry which has a string value (or multiple values). Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose value is to be retrieved.</param>
        /// <returns>A list containing the value(s) of the entry.</returns>
        public List<string> getStringEntryValues(string entryName)
        {
            // check if entry name exists in sscStringEntries
            if (sscStringEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscStringEntries[entryName].Values.ToList();
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns the value(s) of a entry which has a numeric value (or multiple values). Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose value is to be retrieved.</param>
        /// <returns>A list containing the value(s) of the entry.</returns>
        public List<double> getNumericEntryValues(string entryName)
        {
            // check if entry name exists in sscNumericEntries
            if (sscNumericEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscNumericEntries[entryName].Values.ToList();
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns the value(s) of a entry which has a boolean value (or multiple values). Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose value is to be retrieved.</param>
        /// <returns>A list containing the value(s) of the entry.</returns>
        public List<bool> getBooleanEntryValues(string entryName)
        {
            // check if entry name exists in sscBooleanEntries
            if (sscBooleanEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscBooleanEntries[entryName].Values.ToList();
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        #endregion

        #region DESCRIPTION VALUE GETTERS

        /// <summary>
        /// Returns the description of a entry with a string-based value. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose description is to be retrieved.</param>
        public string getStringEntryDescription(string entryName)
        {
            // check if entry name exists in sscStringEntries
            if (sscStringEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscStringEntries[entryName].Description;
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns the description of a entry with a numerical-based value. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose description is to be retrieved.</param>
        public string getNumericEntryDescription(string entryName)
        {
            // check if entry name exists in sscNumericEntries
            if (sscNumericEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscNumericEntries[entryName].Description;
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        /// <summary>
        /// Returns the description of a entry with a boolean-based value. Returns null if entry not found.
        /// </summary>
        /// <param name="entryName">The name of the entry whose description is to be retrieved.</param>
        public string getBooleanEntryDescription(string entryName)
        {
            // check if entry name exists in sscBooleanEntries
            if (sscBooleanEntries.ContainsKey(entryName))
            {
                // return entry values
                return sscBooleanEntries[entryName].Description;
            }
            else
            {
                // entry not found; return null
                return null;
            }
        }

        #endregion

        #endregion

        #region SETTER FUNCTIONS (for values of entries)

        /// <summary>
        /// Attempts to add a string entry to the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="newEntry">String entry to be added.</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool addStringEntry(sscStringEntry newEntry, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (!sscStringEntries.ContainsKey(newEntry.Name))
            {
                // add entry to sscStringEntries
                sscStringEntries.Add(newEntry.Name, newEntry);

                // add to master list of entries
                sscEntriesMasterList.Add(new Tuple<string, EntryType>(newEntry.Name, EntryType.String));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! String entry with name '" + newEntry.Name + "' already exists! Unable to add new entry.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to add a numeric entry to the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="newEntry">Numeric entry to be added</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool addNumericEntry(sscNumericEntry newEntry, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (!sscNumericEntries.ContainsKey(newEntry.Name))
            {
                // add entry to sscStringEntries
                sscNumericEntries.Add(newEntry.Name, newEntry);

                // add to master list of entries
                sscEntriesMasterList.Add(new Tuple<string, EntryType>(newEntry.Name, EntryType.Numeric));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! Numeric entry with name '" + newEntry.Name + "' already exists! Unable to add new entry.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to add a boolean entry to the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="newEntry">Boolean entry to be added</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool addBooleanEntry(sscBooleanEntry newEntry, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (!sscBooleanEntries.ContainsKey(newEntry.Name))
            {
                // add entry to sscStringEntries
                sscBooleanEntries.Add(newEntry.Name, newEntry);

                // add to master list of entries
                sscEntriesMasterList.Add(new Tuple<string, EntryType>(newEntry.Name, EntryType.Boolean));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! Boolean entry with name '" + newEntry.Name + "' already exists! Unable to add new entry.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to delete a string entry from the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="entryToDeleteName">Name of the string entry to be deleted.</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool deleteStringEntry(string entryToDeleteName, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (sscStringEntries.ContainsKey(entryToDeleteName))
            {
                // add entry to sscStringEntries
                sscStringEntries.Remove(entryToDeleteName);

                // add to master list of entries
                sscEntriesMasterList.Remove(new Tuple<string, EntryType>(entryToDeleteName, EntryType.String));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! String entry with name '" + entryToDeleteName + "' doesn't exist! Unable to delete entry.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to delete a numeric entry from the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="entryToDelete">Name of the numeric entry to be deleted</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool deleteNumericEntry(string entryToDeleteName, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (sscNumericEntries.ContainsKey(entryToDeleteName))
            {
                // add entry to sscStringEntries
                sscNumericEntries.Remove(entryToDeleteName);

                // add to master list of entries
                sscEntriesMasterList.Remove(new Tuple<string, EntryType>(entryToDeleteName, EntryType.Numeric));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! Numeric entry with name '" + entryToDeleteName + "' doesn't exist! Unable to delete entry.";
                return false;
            }
        }

        /// <summary>
        /// Attempts to delete a boolean entry from the SSC. Returns a boolean value indicating if the operation succeeded.
        /// If any errors occurred the error message will be returned in the errorMessage parameter.
        /// </summary>
        /// <param name="entryToDelete">Name of boolean entry to be deleted</param>
        /// <param name="errorMessage">If errors occur during function, error message will be populated in this variable.</param>
        /// <returns>Returns a boolean value indicating if the operation succeeded. Details of failure are populate in errorMessage if they occur.</returns>
        public bool deleteBooleanEntry(string entryToDeleteName, out string errorMessage)
        {
            // check if entry name already exists in string entries
            if (sscBooleanEntries.ContainsKey(entryToDeleteName))
            {
                // add entry to sscStringEntries
                sscBooleanEntries.Remove(entryToDeleteName);

                // add to master list of entries
                sscEntriesMasterList.Remove(new Tuple<string,EntryType>(entryToDeleteName, EntryType.Boolean));

                errorMessage = "";
                return true;
            }
            else
            {
                errorMessage = "Error! Boolean entry with name '" + entryToDeleteName + "' doesn't exist! Unable to delete entry.";
                return false;
            }
        }

        /// <summary>
        /// Updates the name of the entry with the name defined by originalName. Returns a boolean indicating if operation was
        /// successful or not (if originalName was found in entries list AND is the same type of entry as entryType defines).
        /// If an error occurs the error message is returned in the errorMessage parameter.
        /// </summary>
        /// <param name="originalName">The original name of the entry being renamed.</param>
        /// <param name="entryType">The type of entry that is being changed.</param>
        /// <param name="newName">The name to update the entry to.</param>
        /// <param name="errorMessage">Stores error messages (if errors occur during the operation). If no errors occurred string will be blank.</param>
        /// <returns>Returns a boolean indicating if operation was successful or not (if originalName was 
        /// found in entries list AND is the same type of entry as entryTyped defines)</returns>
        public bool updateEntryName(string originalName, EntryType entryType, string newName, out string errorMessage)
        {
            // ensure that entry with newName doesn't already exist
            if (sscEntriesMasterList.Contains(new Tuple<string, EntryType>(newName, entryType)))
            {
                // can't update value since entry already exists. Return error message
                errorMessage = "Error! Could not update value since entry '" + newName + "' of type '" + entryType.ToString() + "' already exists.";
                return false;
            }

            // check if there is a matching entry (same name and type of entry)
            for (int m = 0; m < sscEntriesMasterList.Count; m++)
            {
                // check if type matches
                if (sscEntriesMasterList[m].Item2 == entryType)
                {
                    // check if name matches
                    if (sscEntriesMasterList[m].Item1 == originalName)
                    {
                        // entry found; update it in master list
                        sscEntriesMasterList[m] = new Tuple<string, EntryType>(
                            newName, entryType);

                        // update entry in type-specific entry collection
                        if (entryType == EntryType.String)
                        {
                            #region STRING ENTRY
                            // copy entry into new entry object (with new name)
                            sscStringEntry newEntry = new sscStringEntry(newName, 
                                sscStringEntries[originalName].Description,
                                sscStringEntries[originalName].Values.ToList());

                            // remove old item from dictionary
                            sscStringEntries.Remove(originalName);

                            // add new item to dictionary (if key already exists operation failed)
                            sscStringEntries.Add(newName, newEntry);

                            #endregion
                        }
                        else if (entryType == EntryType.Numeric)
                        {
                            #region NUMERIC ENTRY
                            // copy entry into new entry object (with new name)
                            sscNumericEntry newEntry = new sscNumericEntry(newName,
                                sscNumericEntries[originalName].Description,
                                sscNumericEntries[originalName].Values.ToList());

                            // remove old item from dictionary
                            sscNumericEntries.Remove(originalName);

                            // add new item to dictionary (if key already exists operation failed)
                            sscNumericEntries.Add(newName, newEntry);
                            #endregion
                        }
                        else if (entryType == EntryType.Boolean)
                        {
                            #region BOOLEAN ENTRY
                            // copy entry into new entry object (with new name)
                            sscBooleanEntry newEntry = new sscBooleanEntry(newName,
                                sscBooleanEntries[originalName].Description,
                                sscBooleanEntries[originalName].Values.ToList());

                            // remove old item from dictionary
                            sscBooleanEntries.Remove(originalName);

                            // add new item to dictionary (if key already exists operation failed)
                            sscBooleanEntries.Add(newName, newEntry);
                            #endregion
                        }

                        errorMessage = "";
                        return true;
                    }
                }
            }

            errorMessage = "Error! Entry '" + originalName + "' of type '" + entryType.ToString() + "' not found, could not update entry name.";
            return false;
        }

        /// <summary>
        /// Sets the values of a entry that contains string value(s). Returns true if successful, false if not [likely due to entry not existing or newValues being null].
        /// </summary>
        /// <param name="entryName">Name of the entry whose value(s) are to be changed.</param>
        /// <param name="newValues">The new value(s) that the entry should be changed to.</param>
        /// <returns>Boolean indicating whether update was successful.</returns>
        public bool updateStringEntryValues(string entryName, string[] newValues)
        {
            // check if entry name exists in sscStringEntries AND newValues is not null
            if (sscStringEntries.ContainsKey(entryName) && newValues != null)
            {
                // update values by creating new string entry struct with new values
                sscStringEntries[entryName] = new sscStringEntry(entryName,
                    sscStringEntries[entryName].Description, newValues);
                return true;
            }

            // item not found, return false
            return false;
        }

        /// <summary>
        /// Sets the values of a entry that contains numeric value(s). Returns true if successful, false if not [likely due to entry not existing or newValues being null].
        /// </summary>
        /// <param name="entryName">Name of the entry whose value(s) are to be changed.</param>
        /// <param name="newValues">The new value(s) that the entry should be changed to.</param>
        /// <returns>Boolean indicating whether update was successful.</returns>
        public bool updateNumericEntryValues(string entryName, double[] newValues)
        {
            // check if entry name exists in sscStringEntries AND newValues is not null
            if (sscNumericEntries.ContainsKey(entryName) && newValues != null)
            {
                // update values by creating new string entry struct with new values
                sscNumericEntries[entryName] = new sscNumericEntry(entryName,
                    sscNumericEntries[entryName].Description, newValues);
                return true;
            }

            // item not found, return false
            return false;
        }

        /// <summary>
        /// Sets the values of a entry that contains boolean value(s). Returns true if successful, false if not [likely due to entry not existing or newValues being null].
        /// </summary>
        /// <param name="entryName">Name of the entry whose value(s) are to be changed.</param>
        /// <param name="newValues">The new value(s) that the entry should be changed to.</param>
        /// <returns>Boolean indicating whether update was successful.</returns>
        public bool updateBooleanEntryValues(string entryName, bool[] newValues)
        {
            // check if entry name exists in sscStringEntries AND newValues is not null
            if (sscBooleanEntries.ContainsKey(entryName) && newValues != null)
            {
                // update values by creating new string entry struct with new values
                sscBooleanEntries[entryName] = new sscBooleanEntry(entryName,
                    sscBooleanEntries[entryName].Description, newValues);
                return true;
            }

            // item not found, return false
            return false;
        }

        #endregion
    }
}
