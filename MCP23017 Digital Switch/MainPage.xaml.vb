' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
''' 
Imports System
Imports System.Threading
Imports Windows.UI.Xaml.Controls
Imports Windows.Devices.Enumeration
Imports Windows.Devices.I2C
Imports Windows.Devices.Gpio


Public NotInheritable Class MainPage
    Inherits Page


    Private periodicTimer As Timer
    Private I2C_IOexpander As Windows.Devices.I2C.I2cDevice

    Dim portGPA0 As Boolean
    Dim portGPA1 As Boolean


    Private Async Sub InitI2C_MCP23017()



        portGPA0 = False
        portGPA1 = False

        Dim aqs As String = I2cDevice.GetDeviceSelector()       'Get a selector string that will return all I2C controllers on the system 
        Dim dis = Await DeviceInformation.FindAllAsync(aqs)     'Find the I2C bus controller device With our selector String           

        If (dis.Count = 0) Then
            Text_Status.Text = "No I2C controllers were found on the system"
            Return
        End If

        Dim settings = New I2cConnectionSettings(&H20)



        settings.BusSpeed = I2cBusSpeed.FastMode
        I2C_IOexpander = Await I2cDevice.FromIdAsync(dis(0).Id, settings)    'Create an I2cDevice With our selected bus controller And I2C settings 
        If (I2C_IOexpander Is Nothing) Then
            Text_Status.Text = String.Format("Slave address {0} on I2C Controller {1} is currently in use by another application. Please ensure that no other applications are using I2C.", settings.SlaveAddress, dis(0).Id)
            Return
        End If


        ' Initialize the IO Expander:
        ' For this device, we create 2-byte write buffers:
        ' The first byte Is the register address we want to write to.
        ' The second byte Is the contents that we want to write to the register. 

        Dim WriteBuf_IODIRA As Byte() = {&H0, &H80} ' 0x0 All ports in Bank 1 are output ports
        Dim WriteBuf_OLATA As Byte() = {&H14, &H1} ' Select OLATA register and turn on  GPA0 

        ' Write the register settings 
        Try
            I2C_IOexpander.Write(WriteBuf_IODIRA)
            I2C_IOexpander.Write(WriteBuf_OLATA)
        Catch ex As Exception
            ' If the write fails display the error And stop running 
            Text_Status.Text = "Failed to communicate with device: " & ex.Message
            Return
        End Try



        Dim TimerDelegate As New System.Threading.TimerCallback(AddressOf TimerCallback)
        'Now that everything Is initialized, its time to create a timer so we read data every 100mS 
        periodicTimer = New Timer(TimerDelegate, Nothing, 0, 100)
    End Sub


    Private Sub TimerCallback(state As Object)
        '/* UI updates must be invoked on the UI thread */


        Dim WriteBuf_OLATA As Byte()

        If portGPA0 = False Then
            WriteBuf_OLATA = {&H14, &H1} 'LED blink
            portGPA0 = True
            portGPA1 = False
        Else
            WriteBuf_OLATA = {&H14, &H2} 'Other LED blink
            portGPA0 = False
            portGPA1 = True
        End If

        I2C_IOexpander.Write(WriteBuf_OLATA)

        'Read Input Ports from Bank 0
        Dim ReadBuf As Byte() = New Byte(1) {}
        Dim RegAddrBuf As Byte() = New Byte() {&H13}

        I2C_IOexpander.WriteRead(RegAddrBuf, ReadBuf)



        Dim Task = Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                             Text_X_Axis.Text = Date.Now
                                                                                             Text_Y_Axis.Text = ReadBuf(0)
                                                                                             Text_Status.Text = "Running"
                                                                                         End Sub)
    End Sub

    Private Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        InitI2C_MCP23017()
    End Sub
End Class
