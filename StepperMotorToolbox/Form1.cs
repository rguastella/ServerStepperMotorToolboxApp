using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace StepperMotorToolbox
{

    public partial class StepperMotorToolboxForm : Form
    {
        SerialPort serialPort = new SerialPort();
        public string[] ports = SerialPort.GetPortNames();

        UInt32 xactualValue;

        string proRecMessageIdentifier;
        string serialRecBuffer;

        string[] procRecMessage_ID = new string[]
        {
            "NO_MESSAGE_TO_PROCESS",
            "PROC_SPI_REG_READ",
            "PROC_MTR_CTRL_VOLT_READ",
            "PROC_MTR_CTRL_ACCEL_READ",
            "PROC_MTR_CTRL_RUN_BUTTON",
            "PROC_MTR_XACTUAL_READ",
            "PROC_MTR_STOP_BUTTON",
        };


        string[] spiRegisterName = new string[]
        {
              "GCONF", "GSTAT", "IFCNT", "SLAVECONF", "IO_INPUT_OUTPUT", "X_COMPARE", "OTP_PROG", "OTP_READ", "FACTORY_CONF", "SHORT_CONF", "DRV_CONF",
              "GLOBAL_SCALER",  "OFFSET_READ",

              /* Velocity dependent driver feature control registers */
              "IHOLD_IRUN", "TPOWERDOWN", "TSTEP", "TPWMTHRS", "TCOOLTHRS", "THIGH", 

  /* Ramp generator motion control registers */
              "RAMPMODE", "XACTUAL", "VACTUAL", "VSTART", "A_1", "V_1", "AMAX", "VMAX", "DMAX", "D_1",

    //Attention:  Do  not  set  0  in  positioning  mode, even if V1=0!
              "VSTOP",
    //Attention: Set VSTOP > VSTART!
    //Attention:  Do  not  set  0  in  positioning  mode, minimum 10 recommend!
             "TZEROWAIT", "XTARGET",

              /* Ramp generator driver feature control registers */
              "VDCMIN", "SW_MODE", "RAMP_STAT", "XLATCH", 

              /* Encoder registers */
              "ENCMODE", "X_ENC", "ENC_CONST", "ENC_STATUS", "ENC_LATCH", "ENC_DEVIATION",

              /* Motor driver registers */
              "MSLUT_0_7", "MSLUTSEL", "MSLUTSTART", "MSCNT", "MSCURACT", "CHOPCONF", "COOLCONF", "DCCTRL", "DRV_STATUS",
              "PWMCONF", "PWM_SCALE", "PWM_AUTO", "LOST_STEPS",

            "LAST_STEPPER_ADDR",
        };

        byte[] spiRegisterNumber = new byte[]
        {
             /* General configuration registers */
          0, // Global configuration flags
          1, // Global status flags
          2, // UART transmission counter
          3, // UART slave configuration
          4, // Read input / write output pins
          5, // Position comparison register
          6, // OTP programming register
          7, // OTP read register
          8, // Factory configuration (clock trim)
          9, // Short detector configuration
          10, // Driver configuration
          11, // Global scaling of motor current
          12, // Offset calibration results

          /* Velocity dependent driver feature control registers */
          16, // Driver current control
          17, // Delay before power down
          18, // Actual time between microsteps
          19, // Upper velocity for stealthChop voltage PWM mode
          20, // Lower threshold velocity for switching on smart energy coolStep and stallGuard feature
          21, // Velocity threshold for switching into a different chopper mode and fullstepping

          /* Ramp generator motion control registers */
          32, // Driving mode (Velocity, Positioning, Hold)
          33, // Actual motor position
          34, // Actual  motor  velocity  from  ramp  generator
          35, // Motor start velocity
          36, // First acceleration between VSTART and V1
          37, // First acceleration/deceleration phase target velocity
          38, // Second acceleration between V1 and VMAX
          39, // Target velocity in velocity mode
          40, // Deceleration between VMAX and V1
          42, // Deceleration between V1 and VSTOP
            //Attention:  Do  not  set  0  in  positioning  mode, even if V1=0!
          43, // Motor stop velocity
            //Attention: Set VSTOP > VSTART!
            //Attention:  Do  not  set  0  in  positioning  mode, minimum 10 recommend!
          44, // Waiting time after ramping down to zero velocity before next movement or direction inversion can start.
          45, // Target position for ramp mode

          /* Ramp generator driver feature control registers */
          51, // Velocity threshold for enabling automatic commutation dcStep
          52, // Switch mode configuration
          53, // Ramp status and switch event status
          54, // Ramp generator latch position upon programmable switch event

          /* Encoder registers */
          56, // Encoder configuration and use of N channel
          57, // Actual encoder position
          58, // Accumulation constant
          59, // Encoder status information
          60, // Encoder position latched on N event
          61, // Maximum number of steps deviation between encoder counter and XACTUAL for deviation warning

          /* Motor driver registers */
          0x96, // Microstep table entries. Add 0...7 for the next registers
          104, // Look up table segmentation definition
          105, // Absolute current at microstep table entries 0 and 256
          106, // Actual position in the microstep table
          107, // Actual microstep current
          108, // Chopper and driver configuration
          109, // coolStep smart current control register and stallGuard2 configuration
          110, // dcStep automatic commutation configuration register
          111, // stallGuard2 value and driver error flags
          112, // stealthChop voltage PWM mode chopper configuration
          113, // Results of stealthChop amplitude regulator.
          114, // Automatically determined PWM config values
          115,  // Number of input steps skipped due to dcStep. only with SD_MODE = 1

          116
        };

        public StepperMotorToolboxForm()
        {
            string[] comPorts;
            serialPort.BaudRate = 115200;
            serialPort.Handshake = Handshake.None;

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived); 

            InitializeComponent();
             
            MotorParametersPanel.Enabled = false;
            MotorControlPanel.Enabled = false;
            SpiBusRegisterPanel.Enabled = false;
            FirmwareDownloadPanel.Enabled = false;

            SerialConnectButton.Enabled = false;
            SpiAlertLabel.Visible = false;

            spiRegSendButton.Enabled = false;
            MicroResetButton.Enabled = false;
            DiagModeButton.Enabled = false;

            FwdRadioButton.Checked = true;
            RevRadioButton.Checked = false;

            // List all the available COM ports in the combo box
            comPorts = System.IO.Ports.SerialPort.GetPortNames();
            ComPortsComboBox.Items.AddRange(comPorts);

            for (int i = 1; i < 7; i++)
            {
                CanAddressComboBox.Items.Add(i);
            }

            for(int i = 0; i < spiRegisterName.Length; i++)
            {
                spiRegisterNameComboBox.Items.Add(spiRegisterName[i]);
            }

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void ExitButton_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort.Close();
            }
            catch
            {

            }
            Application.Exit();
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void ComPortsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComPortsComboBox.Text != "")
            {
                SerialConnectButton.Enabled = true;
            }

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void SerialConnectButton_Click(object sender, EventArgs e)
        {

            if(SerialConnectButton.Text == "Connect")
            {
                if (ComPortsComboBox.Text != "")
                {
                    try
                    {
                        serialPort.PortName = ComPortsComboBox.Text;
                        serialPort.BaudRate = 115200;
                        serialPort.ReadTimeout = 1000;
                        serialPort.WriteTimeout = 1000;

                        // Attach a method to be called when there
                        // is data waiting in the port's buffer

                        serialPort.Open();

                        DiagModeButton.Enabled = true;
                        SerialConnectButton.Text = "Disconnect";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ComPortsComboBox.Text = "";
                    }
                }
            }
            else
            {
                string[] comPorts;

                serialPort.Close();

                MotorParametersPanel.Enabled = false;
                MotorControlPanel.Enabled = false;
                SpiBusRegisterPanel.Enabled = false;
                FirmwareDownloadPanel.Enabled = false;

                SerialConnectButton.Enabled = false;
                SpiAlertLabel.Visible = false;

                spiRegSendButton.Enabled = false;
                MicroResetButton.Enabled = false;
                DiagModeButton.Enabled = false;

                // List all the available COM ports in the combo box
                ComPortsComboBox.Items.Clear();
                comPorts = System.IO.Ports.SerialPort.GetPortNames();
                ComPortsComboBox.Items.AddRange(comPorts);

                for (int i = 1; i < 7; i++)
                {
                    CanAddressComboBox.Items.Add(i);
                }

                SerialConnectButton.Text = "Connect";
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                serialRecBuffer = serialPort.ReadLine();
            }
            catch
            {
                
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiRegisterNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (spiRegisterNameComboBox.Text != "")
            {
                byte spiRegValue = spiRegisterNumber[spiRegisterNameComboBox.SelectedIndex];
                string hex = string.Format("{0:d}", spiRegValue);

                spiRegValueTtextBox.Text =  hex.ToString().ToUpper();

                if((spiWriteRadioButton.Checked) || (spiReadRadioButton.Checked))
                {
                    if(spiWriteRadioButton.Checked)
                    {
                        if(spiDataSendTtextBox.Text != "")
                        {
                            spiRegSendButton.Enabled = true;
                        }
                        else
                        {
                            spiRegSendButton.Enabled = false;
                        }
                    }
                    else
                    {
                        spiRegSendButton.Enabled = true;
                    }
                }
                else
                {
                    spiRegSendButton.Enabled = false;
                }
            }

            if((!spiWriteRadioButton.Checked) && (!spiReadRadioButton.Checked))
            {
                SpiAlertLabel.Visible = true;

            }
            else
            {
                SpiAlertLabel.Visible = false;
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiWriteRadioButton_MouseClick(object sender, MouseEventArgs e)
        {
            if((spiDataSendTtextBox.Text != "") && (spiRegisterNameComboBox.Text != ""))
            {
                spiRegSendButton.Enabled = true;
            }
            else
            {
                spiRegSendButton.Enabled = false;
            }
            SpiAlertLabel.Visible = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiReadRadioButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (spiRegisterNameComboBox.Text != "")
            {
                spiRegSendButton.Enabled = true;
            }
            else
            {
                spiRegSendButton.Enabled = false;
            }
            SpiAlertLabel.Visible = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void MotorParmEnableButton_Click(object sender, EventArgs e)
        {
            InnerMotorParameterPanel.Enabled = true;
            InnerFirmwareDownloadPanel.Enabled = false;
            InnerMotorControlPanel.Enabled = false;
            InnerSpiBusRegisterPanel.Enabled = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void MotorCtrlEnableButton_Click(object sender, EventArgs e)
        {
            InnerMotorControlPanel.Enabled = true;
            InnerFirmwareDownloadPanel.Enabled = false;
            InnerMotorParameterPanel.Enabled = false;
            InnerSpiBusRegisterPanel.Enabled = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void button1_Click(object sender, EventArgs e)
        {
            InnerFirmwareDownloadPanel.Enabled = true;
            InnerMotorControlPanel.Enabled = false;
            InnerMotorParameterPanel.Enabled = false;
            InnerSpiBusRegisterPanel.Enabled = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void button2_Click(object sender, EventArgs e)
        {
            InnerSpiBusRegisterPanel.Enabled = true;
            InnerFirmwareDownloadPanel.Enabled = false;
            InnerMotorControlPanel.Enabled = false;
            InnerMotorParameterPanel.Enabled = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void EepromEnableButton_Click(object sender, EventArgs e)
        {
            InnerMotorControlPanel.Enabled = false;
            InnerFirmwareDownloadPanel.Enabled = false;
            InnerMotorParameterPanel.Enabled = false;
            InnerSpiBusRegisterPanel.Enabled = false;

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiRegSendButton_Click(object sender, EventArgs e)
        {
            string buffer;

            if(spiWriteRadioButton.Checked)
            {
                buffer = "writeSpi," + spiRegValueTtextBox.Text + "," + spiDataSendTtextBox.Text + ",";
                serialPort.WriteLine(buffer);

            }
            else if(spiReadRadioButton.Checked)
            {
                proRecMessageIdentifier = "PROC_SPI_REG_READ";
                buffer = "readSpi," + spiRegValueTtextBox.Text + ",";
                serialPort.WriteLine(buffer);
            }
      }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiDataRecTextBox_Leave(object sender, EventArgs e)
        {
            UInt32 value;

            try
            {
                value = Convert.ToUInt32(spiDataRecTextBox.Text, 10);
                spiDataRecTextBox.Text = spiDataRecTextBox.Text.ToUpper();
            }
            catch
            {
                if(spiDataRecTextBox.Text != "")
                {
                    MessageBox.Show("Invalid entry, try again");
                }
            }
         }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void SysTimer_Tick(object sender, EventArgs e)
        {
            if (proRecMessageIdentifier == "PROC_SPI_REG_READ")
            {
                string[] buffer = serialRecBuffer.Split(',');
                spiDataRecTextBox.Text = buffer[1];

                proRecMessageIdentifier = "NO_MESSAGE_TO_PROCESS";
                return;
            }

            if (proRecMessageIdentifier == "PROC_MTR_CTRL_VOLT_READ")
            {
                string[] buffer = serialRecBuffer.Split(',');
                V1TextBox.Text = buffer[1];
                VmaxTextBox.Text = buffer[2];
                VstartTextBox.Text = buffer[3];
                VstopTextBox.Text = buffer[4];

                proRecMessageIdentifier = "NO_MESSAGE_TO_PROCESS";
                return;
            }

            if (proRecMessageIdentifier == "PROC_MTR_CTRL_ACCEL_READ")
            {
                string[] buffer = serialRecBuffer.Split(',');
                A1TextBox.Text = buffer[1];
                AmaxTextBox.Text = buffer[2];
                D1TextBox.Text = buffer[3];
                DmaxTextBox.Text = buffer[4];

                proRecMessageIdentifier = "NO_MESSAGE_TO_PROCESS";
                return;
            }

            if (proRecMessageIdentifier == "PROC_MTR_XACTUAL_READ")
            {
                string[] buffer = serialRecBuffer.Split(',');
                xactualValue = Convert.ToUInt32(buffer[1]);
                //                getXactualTextBox.Text = buffer[1];
                getXactualTextBox.Text = (xactualValue / 256).ToString();

                proRecMessageIdentifier = "NO_MESSAGE_TO_PROCESS";
                return;
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void spiDataSendTtextBox_Leave(object sender, EventArgs e)
        {
            Int32 value;

            try
            {
                value = Convert.ToInt32(spiDataSendTtextBox.Text, 10);
                spiDataSendTtextBox.Text = spiDataSendTtextBox.Text.ToUpper();
                spiRegSendButton.Enabled = true;

            }
            catch
            {
                if(spiDataSendTtextBox.Text != "")
                {
                    MessageBox.Show("Invalid entry, try again");
                }
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void MicroResetButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "microReset\r\n";
            serialPort.WriteLine(buffer);

            serialPort.Close();
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void InitializeDriverButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "stepDrvInit\r\n";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void DiagModeButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "diagMode,true\r\n";
            serialPort.WriteLine(buffer);

            MotorParametersPanel.Enabled = true;
            MotorControlPanel.Enabled = true;
            SpiBusRegisterPanel.Enabled = true;
            FirmwareDownloadPanel.Enabled = true;

            InnerFirmwareDownloadPanel.Enabled = false;
            InnerMotorControlPanel.Enabled = false;
            InnerMotorParameterPanel.Enabled = false;
            InnerSpiBusRegisterPanel.Enabled = false;

            SysTimer.Enabled = true;
            MicroResetButton.Enabled = true;
            DiagModeButton.Enabled = false;
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void DriveEnableRadioButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "stepDrvEn,true\r\n";
            serialPort.WriteLine(buffer);

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void DriveDisableRadioButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "stepDrvDis,true\r\n";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void ResetStepButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "stepDrvReset,\r\n";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void VoltWriteButton_Click(object sender, EventArgs e)
        {
            string buffer;
            string nokstr = "--,";

            if((V1TextBox.Text != "") && (VmaxTextBox.Text != "") && (VstartTextBox.Text != "") && (VstopTextBox.Text != ""))
            {
                buffer = "ctrlVoltsWriteParams," + V1TextBox.Text + "," + VmaxTextBox.Text + "," + VstartTextBox.Text + "," + VstopTextBox.Text + ",";
                serialPort.WriteLine(buffer);
            }
            else
            {
                serialPort.WriteLine(nokstr);
            }
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void VoltReadbutton_Click(object sender, EventArgs e)
        {
            string buffer;

            proRecMessageIdentifier = "PROC_MTR_CTRL_VOLT_READ";
            buffer = "ctrlVoltsReadParams,";
            serialPort.WriteLine(buffer);

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void AccelReadButton_Click(object sender, EventArgs e)
        {
            string buffer;

            proRecMessageIdentifier = "PROC_MTR_CTRL_ACCEL_READ";
            buffer = "ctrlAccelReadParams,";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void AccelWriteButton_Click(object sender, EventArgs e)
        {
            string buffer;
            string nokstr = "--,";

            if ((A1TextBox.Text != "") && (AmaxTextBox.Text != "") && (D1TextBox.Text != "") && (DmaxTextBox.Text != ""))
            {
                buffer = "ctrlAccelWriteParams," + A1TextBox.Text + "," + AmaxTextBox.Text + "," + D1TextBox.Text + "," + DmaxTextBox.Text + ",";
                serialPort.WriteLine(buffer);
            }
            else
            {
                serialPort.WriteLine(nokstr);
            }

        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void mtrCtrlRunButton_Click(object sender, EventArgs e)
        {
 


        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void FwdRadioButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "motorDirection, forward,";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
         * 
         * -----------------------------------------------------------*/
        private void RevRadioButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "motorDirection, reverse,";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
        * 
        * -----------------------------------------------------------*/
        private void GetXactualButton_Click(object sender, EventArgs e)
        {
            string buffer;
            xactualValue = 0;

            proRecMessageIdentifier = "PROC_MTR_XACTUAL_READ";

            buffer = "readSpi,33,";
            serialPort.WriteLine(buffer);
        }

        /*-------------------------------------------------------------
        * 
        * -----------------------------------------------------------*/
        private void Run2TargetButton_Click(object sender, EventArgs e)
        {
            string buffer;
            Int32 xTargetValue;

            if(xtargetEnterTextBox.Text != "")
            {
                xTargetValue = Convert.ToInt32( xtargetEnterTextBox.Text ) * 256;
                if(FwdRadioButton.Checked == true)
                {
                    xTargetValue = (Int32)xactualValue + xTargetValue;

                    buffer = "writeSpi,45," + xTargetValue.ToString() + ",";
                    serialPort.WriteLine(buffer);
                }
                else
                {
                    xTargetValue = (Int32)xactualValue - xTargetValue;

                    buffer = "writeSpi,45," + xTargetValue.ToString() + ",";
                    serialPort.WriteLine(buffer);
                }

            }
            else
            {
                MessageBox.Show("Enter a Target Value");
            }
        }

        /*-------------------------------------------------------------
        * 
        * -----------------------------------------------------------*/
        private void motorStopButton_Click(object sender, EventArgs e)
        {
            string buffer;

            buffer = "stopMotor,";
            serialPort.WriteLine(buffer);
        }
    }
}
