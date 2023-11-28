using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SerialStorageContainerLib;

namespace SSC_TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // declare local variables
            // -container used for testing
            SerialStorageContainer testContainer = new SerialStorageContainer();
            // -used to store error messages from other functions
            string errorMessage = "";
            // -stores the testContainer in serialized format
            string serializedTestContainer = "";

            // String setting
            if (!testContainer.addStringEntry(
                new SerialStorageContainer.sscStringEntry("String Entry #1", "First string entry", "HelloWorld"),
                out errorMessage))
            {
                // display error message if adding string entry #1 failed
                Console.WriteLine(errorMessage);
            }
            else
            {
                Console.WriteLine("Added string entry, 'String Entry #1', to testContainer.");
            }

            // try add a numeric list setting (by array)
            if (!testContainer.addNumericEntry(
                new SerialStorageContainer.sscNumericEntry("Numeric List #1", "First numeric list entry",
                    new double[3] { 3.14, 1.59, 2.65 }),
                out errorMessage))
            {
                // display error message if adding entry failed
                Console.WriteLine(errorMessage);
            }
            else
            {
                Console.WriteLine("Added numeric list entry, 'Numeric List #1', to testContainer.");
            }

            // try add a boolean setting
            if (!testContainer.addBooleanEntry(
                new SerialStorageContainer.sscBooleanEntry("Boolean Entry #1", "First boolean entry",
                    true),
                out errorMessage))
            {
                // display error message if adding entry failed
                Console.WriteLine(errorMessage);
            }
            else
            {
                Console.WriteLine("Added boolean entry, 'Boolean Entry #1', to testContainer.");
            }

            // add spacing
            Console.WriteLine();

            // serialize test container
            serializedTestContainer = testContainer.serializeSSC();

            // indicate option to proceed
            Console.WriteLine("Press any key to proceed.");
            Console.ReadLine();

            // display message to user indicating that SSC in serialized format is displayed below
            Console.WriteLine("testContainer in serialized format:" + Environment.NewLine);
            Console.WriteLine(("").PadLeft(20, '*'));
            // write serialized test container
            Console.WriteLine(serializedTestContainer);

            // padding to indicate end
            Console.WriteLine(("").PadLeft(20, '*') + Environment.NewLine);

            // indicate option to proceed
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
