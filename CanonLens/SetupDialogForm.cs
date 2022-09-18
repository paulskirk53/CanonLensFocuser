using ASCOM.CanonLens;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace ASCOM.CanonLens
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            Focuser.comPort = (string)comboBoxComPort.SelectedItem;
            tl.Enabled = chkTrace.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            chkTrace.Checked = tl.Enabled;
            // set the list of com ports to those that are currently available
            comboBoxComPort.Items.Clear();
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());      // use System.IO because it's static
            comboBoxComPort.Items.Remove("COM1");                                           //com1 never used by mcus
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(Focuser.comPort))
            {
                comboBoxComPort.SelectedItem = Focuser.comPort;
            }

            //new


            ASCOM.Utilities.Serial tempPort = new ASCOM.Utilities.Serial();     // setup a variable as an ascom utils serial object

            var portlist = new List<string>(tempPort.AvailableCOMPorts);         // create a list of available comports on tempPort
            portlist.Remove("COM1");                                             // COM1 is never used by MCUs

            //now send id messages to each port in the list to find which MCU is attached to which port.

            try
            {

                string portName;
                portName = portFinder(tempPort, "focuser#", portlist);          // this routine returns the port name that replied e.g. COM7

               
                LBLcomPort.Text = checkForNull(portName, "Focuser");
            }

            catch (Exception ex)
            {

                MessageBox.Show(" connection failed. Check the MCUs are on, connected, and in receive mode." + ex.Message);
            }




        }



        private string checkForNull(string portName, string mcu)
        {
            if (portName == null)
            {
                return mcu + " Unavailable check connection";
            }
            else
            {
                return mcu + " is on " + portName;
            }
        }

        private string portFinder(ASCOM.Utilities.Serial testPort, string mcuName, List<string> portlist)  //mcuName will be e.g "encoder" or "stepper"
        {
            //*
            // * This routine uses a test port to cycle through the portnames (COM1, COM3 etc), checking each port 
            //*  by sending a string recognised by a particular MCU e.g. stepper# or encoder#
            //*  if the mcu is on the port, it responds with stepper# or encoder#
            //*
            setupThePort(testPort);            //set the parameters for testport - baud etc
            bool found = false;
            foreach (string portName in portlist)     // GetUnusedSerialPorts forms a list of COM ports which are available
            {
                // MessageBox.Show("the port being checked is " + portName);        //tis worked
                found = checkforMCU(testPort, portName, mcuName);     // this checks if the current portName responds to mcuName (stepper# / emcoder#)
                if (found)
                {
                    //  MessageBox.Show("the port is found " + portName);
                    testPort.Connected = false;                    //disconnect the port
                    return portName;

                }


            }
            return null;                // if no ports respond to queries (e.g. perhaps mcus are not connected), the nukk return is picked by the try - catch exception
                                        // of encoder connect or stepper connect
                                        //  throw new NullReferenceException();
        }

        private bool checkforMCU(ASCOM.Utilities.Serial testPort, string portName, string MCUDescription)
        {

            testPort.PortName = portName;  //                      
            testPort.Connected = true;
            Thread.Sleep(500);           // delay (in mS) - essential if the MCU is Arduino with a bootloader. The Arduino requires time after the port is connected before it can respond to serial requests.

            // send data to the MCU and see what comes back
            try
            {

                // MessageBox.Show("Sending " + MCUDescription + " to port " + portName);

                testPort.Transmit(MCUDescription);                   // transmits encoder# or stepper# depending upon where called


                string response = testPort.ReceiveTerminated("#");   // not all ports respond to a query and those which don't respond will timeout

                // MessageBox.Show("the response from the MCU " + response);

                if (response == MCUDescription)
                {
                    testPort.Connected = false;
                    return true;            //mcu response match
                }
                else
                {
                    testPort.Connected = false;
                    return false;
                }


                // return false;              // if there was a response it was not the right MCU
            }
            catch (Exception e)     //TimeoutException
            {

                testPort.Connected = false;    // no response
                                               // MessageBox.Show("the MCU  did not respond to  " + MCUDescription);
            }

            return false;
        }
        private void setupThePort(ASCOM.Utilities.Serial testPort)
        {
            //set all the port propereties

            testPort.DTREnable = false;
            testPort.RTSEnable = false;
            testPort.ReceiveTimeout = 5;

            testPort.Speed = ASCOM.Utilities.SerialSpeed.ps19200;



        }


        private void comboBoxComPort_SelectionChangeCommitted(object sender, EventArgs e)   // bad name for the combo - this is the selection change for the azimuth comport
        {

            myGlobals.check1 = true;
            overallCheck();
        }

      
        private void overallCheck()      // a way of checking if all the com ports have been picked by the end user.
        {
            if (myGlobals.check1 )
            {
                cmdOK.Enabled = true;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            myGlobals.check1 = true;
            
            overallCheck();
        }



        // end new


    }


    public static class myGlobals     // this is a way of making a global variable set, accessible fom anywhere in the namespace. It's used to determine if all the com ports have been selected.
    {
        public static bool check1 = false;
        
        
    }


}
