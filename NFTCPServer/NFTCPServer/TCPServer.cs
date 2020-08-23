using nanoFramework.Runtime.Events;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace NFTCPServer
{
    /// <summary>
    /// Defines the TCPServer class.
    /// </summary>
    public class TCPServer
    {
        // ###########################################################################################################
        // # Delegates
        // ###########################################################################################################
        #region Delegates

        /// <summary>
        /// Defines the delegate of the event handler for the OnSerialDataReceived event.
        /// </summary>
        /// <param name="sender">The serial driver.</param>
        /// <param name="e">The event arguments.</param>
        public delegate void OnSerialDataReceivedDelegate( object sender, BytesReceivedEventArgs e );

        #endregion Delegates

        // ###########################################################################################################
        // # Properties
        // ###########################################################################################################
        #region Properties

        /// <summary>
        /// Gets or sets the TCP/IP port the server will listen for incoming client connections on.
        /// </summary>
        public int ListeningOnPort { get; set; }

        #endregion Properties

        // ###########################################################################################################
        // # Constructor
        // ###########################################################################################################
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TCPServer class.
        /// </summary>
        public TCPServer()
        {
        }

        #endregion Constructor

        // ###########################################################################################################
        // # Events
        // ###########################################################################################################
        #region Events

        /// <summary>
        /// The event raised by the serial device when bytes have been received.
        /// </summary>
        public event OnSerialDataReceivedDelegate OnSerialDataReceived = delegate { };

        #endregion Events

        // ###########################################################################################################
        // # Methods
        // ###########################################################################################################
        #region Methods

        /// <summary>
        /// Opens the serial port and starts a thread that will monitor the serial port for incoming data.
        /// </summary>
        /// <returns>true if the serial port was successfully opened.</returns>
        public bool Start()
        {
            if ( _listenerThread == null )
            {
                _listenerThread = new Thread( new ThreadStart( ConnectionListenerThread ) );
                _listenerThread.Start();
            }

            return false;
        }

        /// <summary>
        /// Sends an array of bytes through the serial port.
        /// </summary>
        /// <param name="data">The bytes to send.</param>
        /// <param name="length">The number of bytes to send.</param>
        public void WriteBytes( byte[ ] data, int length )
        {
            int offset = 0;

            try
            {
                if ( _clientSocket != null )
                {
                    while ( length > 0 )
                    {
                        int sent = _clientSocket.Send( data );
                        offset += sent;
                        length -= sent;
                        if ( length > 0 )
                        {
                            Array.Copy( data, offset, data, 0, length );
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// This thread monitors the Ethernet interface for client connection.
        /// If a client is connected, the thread waits for client data which is then
        /// passed back through the OnSerialDataReceived event.
        /// </summary>
        private void ConnectionListenerThread()
        {
            byte [] buff = new byte[ 1024 ];
            int received;

            //
            // Grab a collection of available network interfaces.
            //
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();

            //
            // We need at least one interface to continue.
            //
            if ( nis.Length > 0 )
            {
                NetworkInterface ni = nis[0];
                ni.EnableAutomaticDns();
                ni.EnableDhcp();

                //
                // Wait to be assigned an IP address.
                //
                while ( ni.IPv4Address == null || ni.IPv4Address.Length == 0 || ni.IPv4Address.Equals( "0.0.0.0" ) )
                {
                    Thread.Sleep( 100 );
                }

                Debug.WriteLine( $"DHCP has given us the address {ni.IPv4Address},{ni.IPv4SubnetMask},{ni.IPv4GatewayAddress}" );

                while ( true )
                {
                    _clientSocket = null;

                    //
                    // We now have an IP address allocated so we can await
                    // incoming client connections.
                    //
                    IPEndPoint localEndPoint = new IPEndPoint( IPAddress.Any, ListeningOnPort );
                    Socket listener = new Socket( localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp );

                    try
                    {
                        listener.Bind( localEndPoint );
                        listener.Listen( 100 );

                        Debug.WriteLine( $"Waiting for an incoming connection on port {ListeningOnPort}.." );
                        _clientSocket = listener.Accept();
                        Debug.WriteLine( "Connected to a client" );

                        while ( true )
                        {
                            received = _clientSocket.Receive( buff );
                            if ( received > 0 )
                            {
                                OnSerialDataReceived?.Invoke( this, new BytesReceivedEventArgs( buff, received ) );
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        Debug.WriteLine( $"Got a network exception: {ex.Message}" );
                    }
                }
            }
        }

        #endregion Methods

        // ###########################################################################################################
        // # Fields
        // ###########################################################################################################
        #region Fields

        /// <summary>
        /// The thread responsible for receiving serial data over an Ethernet connection.
        /// </summary>
        private Thread _listenerThread;

        /// <summary>
        /// The socket connection to the connected client.
        /// </summary>
        private Socket _clientSocket;

        #endregion Fields
    }

    /// <summary>
    /// Defines the BytesReceivedEventArgs class.
    /// </summary>
    public class BytesReceivedEventArgs : EventArgs
    {
        // ###########################################################################################################
        // # Properties
        // ###########################################################################################################
        #region Properties

        /// <summary>
        /// An array of received bytes.
        /// </summary>
        public byte[ ] Bytes { get; protected set; }

        #endregion Properties

        // ###########################################################################################################
        // # Constructor
        // ###########################################################################################################
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the BytesReceivedEventArgs class.
        /// </summary>
        /// <param name="data">The data bytes that were received.</param>
        /// <param name="length">The length of data received.</param>
        public BytesReceivedEventArgs( byte[ ] data, int length )
        {
            int len = ( length == 0 ) ? data.Length : length;
            Bytes = new byte[ len ];
            Array.Copy( data, Bytes, len );
        }

        #endregion Constructor
    }
}
