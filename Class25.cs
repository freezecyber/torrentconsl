//------------------------------------------------------------------------------ 
// <copyright company="Microsoft" file="IPAddress.cs">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Net
{
    using System.Net.Sockets;
    using System.Globalization;
    using System.Text;
    using System.Security.Permissions;

    /// <devdoc>
    ///    <para>Provides an internet protocol (IP) address.</para> 
    /// </devdoc>
    [Serializable]
    public class IPAddress
    {

        private static readonly IPAddress Any = new IPAddress(0x0000000000000000);
        private static readonly IPAddress Loopback = new IPAddress(0x000000000100007F);
        private static readonly IPAddress Broadcast = new IPAddress(0x00000000FFFFFFFF);
        private static readonly IPAddress None = Broadcast;

        internal const long LoopbackMask = 0x000000000000007F;

        internal static object Parse(string v)
        {
            throw new NotImplementedException();
        }

        internal const string InaddrNoneString = "255.255.255.255";
        internal const string InaddrNoneStringHex = "0xff.0xff.0xff.0xff";
        internal const string InaddrNoneStringOct = "0377.0377.0377.0377";

        //
        // IPv6 Changes: make this internal so other NCL classes that understand about
        //               IPv4 and IPv4 can still access it rather than the obsolete property.
        // 
        internal long m_Address;

        [NonSerialized]
        internal string m_ToString;

        private static readonly IPAddress IPv6Any = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);
        private static readonly IPAddress IPv6Loopback = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 0);
        private static readonly IPAddress IPv6None = new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0);

        /// <devdoc>
        ///   <para> 
        ///     Default to IPv4 address 
        ///   </para>
        /// </devdoc> 
        private AddressFamily m_Family = AddressFamily.InterNetwork;
        private ushort[] m_Numbers = new ushort[NumberOfLabels];
        private long m_ScopeId = 0;                             // really uint !
        private int m_HashCode = 0;

        internal const int IPv4AddressBytes = 4;
        internal const int IPv6AddressBytes = 16;

        internal const int NumberOfLabels = IPv6AddressBytes / 2;


        /// <devdoc>
        ///    <para> 
        ///       Initializes a new instance of the <see cref="System.Net.IPAddress">
        ///       class with the specified 
        ///       address. 
        ///    </see>
        /// </para> 
        private IPAddress(long newAddress)
        {
            if (newAddress < 0 || newAddress > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException("newAddress");
            }
            m_Address = newAddress;
        }


        /// <devdoc> 
        ///    <para>
        ///       Constructor for an IPv6 Address with a specified Scope.
        ///    </para>
        /// </devdoc> 
        private IPAddress(byte[] address, long scopeid)
        {

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length != IPv6AddressBytes)
            {
                
            }

            m_Family = AddressFamily.InterNetworkV6;

            for (int i = 0; i < NumberOfLabels; i++)
            {
                m_Numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
            }

            //
            // Consider: Since scope is only valid for link-local and site-local 
            //           addresses we could implement some more robust checking here
            // 
            if (scopeid < 0 || scopeid > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException("scopeid");
            }

            m_ScopeId = scopeid;
        }
        // 
        private IPAddress(ushort[] address, uint scopeid)
        {
            m_Family = AddressFamily.InterNetworkV6;
            m_Numbers = address;
            m_ScopeId = scopeid;
        }


        /// <devdoc> 
        ///    <para>
        ///       Constructor for IPv4 and IPv6 Address. 
        ///    </para> 
        /// </devdoc>
        private IPAddress(byte[] address)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }
            if (address.Length != IPv4AddressBytes && address.Length != IPv6AddressBytes)
            {
                
            }

            if (address.Length == IPv4AddressBytes)
            {
                m_Family = AddressFamily.InterNetwork;
                m_Address = ((address[3] << 24 | address[2] << 16 | address[1] << 8 | address[0]) & 0x0FFFFFFFF);
            }
            else
            {
                m_Family = AddressFamily.InterNetworkV6;

                for (int i = 0; i < NumberOfLabels; i++)
                {
                    m_Numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
                }
            }
        }

        //
        // we need this internally since we need to interface with winsock 
        // and winsock only understands Int32 
        //
        internal IPAddress(int newAddress)
        {
            m_Address = (long)newAddress & 0x00000000FFFFFFFF;
        }



        /// <devdoc> 
        /// <para>Converts an IP address string to an <see cref="System.Net.IPAddress"> 
        /// instance.</see>
        /// </para> 
        private static bool TryParse(string ipString, out IPAddress address)
        {
            address = InternalParse(ipString, true);
            return (address != null);
        }

      

        public static IPAddress InternalParse(string ipString, bool tryParse)
        {
            if (ipString == null)
            {
                return null;
            }

            // 
            // IPv6 Changes: Detect probable IPv6 addresses and use separate 
            //               parse method.
            // 
            if (ipString.IndexOf(':') != -1)
            {

#if !FEATURE_PAL
                // 
                // If the address string contains the colon character
                // then it can only be an IPv6 address. Use a separate 
                // parse method to unpick it all. Note: we don't support 
                // port specification at the end of address and so can
                // make this decision. 
                //
                // We need to make sure that Socket is initialized for this
                // call !
                // 
                SocketException e = null;
                long scope = 0;
                if (Socket.OSSupportsIPv6)
                {
                    byte[] bytes = new byte[IPv6AddressBytes];
                    SocketAddress saddr = new SocketAddress(AddressFamily.InterNetworkV6);


                    return null;


                    if (tryParse)
                    {
                        return null;
                    }

                    e = new SocketException();
                }
                else
                {
                    unsafe
                    {
                        return null;
                        int offset = 0;
                        if (ipString[0] != '[')
                            ipString = ipString + ']'; //for Uri parser to find the terminator. 
                        else
                            offset = 1;

                        int end = ipString.Length;
                        fixed (char* name = ipString)
                        {
                            
                        }
                    }
                    if (tryParse)
                    {
                        return null;
                    }
                   }

#else // !FEATURE_PAL 
                // IPv6 addresses not supported for FEATURE_PAL 
                throw new SocketException(SocketError.OperationNotSupported);
#endif // !FEATURE_PAL 
            }
            else
            {
                // Cannot be an IPv6 address, so use the IPv4 routines to 
                // parse the string representation.  First check a heuristic to prevent the p/invoke as much as possible.  We
                // know the first character must be a digit (0 for the "0x" in hex), and the last character must be hex. 
                int address = -1;
                if (ipString.Length > 0 && ipString[0] >= '0' && ipString[0] <= '9' &&
                    ((ipString[ipString.Length - 1] >= '0' && ipString[ipString.Length - 1] <= '9') ||
                    (ipString[ipString.Length - 1] >= 'a' && ipString[ipString.Length - 1] <= 'f') ||
                    (ipString[ipString.Length - 1] >= 'A' && ipString[ipString.Length - 1] <= 'F')))
                {
                   
                }
#if BIGENDIAN
                // OSSOCK.inet_addr always returns IP address as a byte array converted to int.
                // thus we need to convert the address into a uniform integer value. 
                address = (int)( ((uint)address << 24) | (((uint)address & 0x0000FF00) << 8) |
                (((uint)address >> 8) & 0x0000FF00) | ((uint)address >> 24) );
#endif

                if (address == -1
                    && string.Compare(ipString, InaddrNoneString, StringComparison.Ordinal) != 0
                    && string.Compare(ipString, InaddrNoneStringHex, StringComparison.OrdinalIgnoreCase) != 0
                    && string.Compare(ipString, InaddrNoneStringOct, StringComparison.Ordinal) != 0)
                {

                    if (tryParse)
                    {
                        return null;
                    }

                     }

                IPAddress returnValue = new IPAddress(address);

                return returnValue;
            }
        } // Parse



        /** 
         * @deprecated IPAddress.Address is address family dependant, use Equals method for comparison.
         */
        /// <devdoc>
        ///     <para> 
        ///         Mark this as deprecated.
        ///     </para> 
        /// </devdoc> 
        [Obsolete("This property has been deprecated. It is address family dependent. Please use IPAddress.Equals method to perform comparisons. http://go.microsoft.com/fwlink/?linkid=14202")]
        private long Address
        {
            get
            {
                //
                // IPv6 Changes: Can't do this for IPv6, so throw an exception.
                // 
                //
                if (m_Family == AddressFamily.InterNetworkV6)
                {
                    return m_Address;
                }
                else
                {
                    return m_Address;
                }
            }
            set
            {
                //
                // IPv6 Changes: Can't do this for IPv6 addresses 
                if (m_Family == AddressFamily.InterNetworkV6)
                {
                    
                }
                else
                {
                    if (m_Address != value)
                    {
                        m_ToString = null;
                        m_Address = value;
                    }
                }
            }
        }

        /// <devdoc>
        /// <para>
        /// Provides a copy of the IPAddress internals as an array of bytes.
        /// </para> 
        /// </devdoc>
        private byte[] GetAddressBytes()
        {
            byte[] bytes;
            if (m_Family == AddressFamily.InterNetworkV6)
            {
                bytes = new byte[NumberOfLabels * 2];

                int j = 0;
                for (int i = 0; i < NumberOfLabels; i++)
                {
                    bytes[j++] = (byte)((this.m_Numbers[i] >> 8) & 0xFF);
                    bytes[j++] = (byte)((this.m_Numbers[i]) & 0xFF);
                }
            }
            else
            {
                bytes = new byte[IPv4AddressBytes];
                bytes[0] = (byte)(m_Address);
                bytes[1] = (byte)(m_Address >> 8);
                bytes[2] = (byte)(m_Address >> 16);
                bytes[3] = (byte)(m_Address >> 24);
            }
            return bytes;
        }

        private AddressFamily AddressFamily
        {
            get
            {
                return m_Family;
            }
        }

        /// <devdoc> 
        ///    <para> 
        ///        IPv6 Scope identifier. This is really a uint32, but that isn't CLS compliant
        ///    </para> 
        /// </devdoc>
        private long ScopeId
        {
            get
            {
                // 
                // Not valid for IPv4 addresses
                // 
                if (m_Family == AddressFamily.InterNetwork)
                {
                    
                }

                return m_ScopeId;
            }
            set
            {
                //
                // Not valid for IPv4 addresses 
                // 
                if (m_Family == AddressFamily.InterNetwork)
                {
                    
                }

                //
                // Consider: Since scope is only valid for link-local and site-local 
                //           addresses we could implement some more robust checking here
                // 
                if (value < 0 || value > 0x00000000FFFFFFFF)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (m_ScopeId != value)
                {
                    m_Address = value;
                    m_ScopeId = value;
                }
            }
        }

        /// <devdoc>
        ///    <para> 
        ///       Converts the Internet address to either standard dotted quad format
        ///       or standard IPv6 representation.
        ///    </para>
        /// </devdoc> 
       public override string ToString()
        {
            if (m_ToString == null)
            {
                // 
                // IPv6 Changes: generate the IPV6 representation
                // 
                if (m_Family == AddressFamily.InterNetworkV6)
                {

#if !FEATURE_PAL
                    int addressStringLength = 256;
                    StringBuilder addressString = new StringBuilder(addressStringLength);

                    if (Socket.OSSupportsIPv6)
                    {
                        SocketAddress saddr = new SocketAddress(AddressFamily.InterNetworkV6);
                        int j = 8;
                        for (int i = 0; i < NumberOfLabels; i++)
                        {
                            saddr[j++] = (byte)(this.m_Numbers[i] >> 8);
                            saddr[j++] = (byte)(this.m_Numbers[i]);
                        }
                        if (m_ScopeId > 0)
                        {
                            saddr[24] = (byte)m_ScopeId;
                            saddr[25] = (byte)(m_ScopeId >> 8);
                            saddr[26] = (byte)(m_ScopeId >> 16);
                            saddr[27] = (byte)(m_ScopeId >> 24);
                        }
                       
                        
                    }
                    else
                    {
                        const string numberFormat = "{0:x4}";
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[0])).Append(':');
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[1])).Append(':');
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[2])).Append(':');
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[3])).Append(':');
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[4])).Append(':');
                        addressString.Append(String.Format(CultureInfo.InvariantCulture, numberFormat, m_Numbers[5])).Append(':');
                        addressString.Append((m_Numbers[6] >> 8) & 0xFF).Append('.');
                        addressString.Append(m_Numbers[6] & 0xFF).Append('.');
                        addressString.Append((m_Numbers[7] >> 8) & 0xFF).Append('.');
                        addressString.Append(m_Numbers[7] & 0xFF);
                        if (m_ScopeId != 0)
                        {
                            addressString.Append('%').Append((uint)m_ScopeId);
                        }
                    }

                    m_ToString = addressString.ToString();
#else // !FEATURE_PAL 
                    // IPv6 addresses not supported for FEATURE_PAL
                    throw new SocketException(SocketError.OperationNotSupported); 
#endif // !FEATURE_PAL
                }
                else
                {
                    unsafe
                    {
                        const int MaxSize = 15;
                        int offset = MaxSize;
                        char* addressString = stackalloc char[MaxSize];
                        int number = (int)((m_Address >> 24) & 0xFF);
                        do
                        {
                            addressString[--offset] = (char)('0' + number % 10);
                            number = number / 10;
                        } while (number > 0);
                        addressString[--offset] = '.';
                        number = (int)((m_Address >> 16) & 0xFF);
                        do
                        {
                            addressString[--offset] = (char)('0' + number % 10);
                            number = number / 10;
                        } while (number > 0);
                        addressString[--offset] = '.';
                        number = (int)((m_Address >> 8) & 0xFF);
                        do
                        {
                            addressString[--offset] = (char)('0' + number % 10);
                            number = number / 10;
                        } while (number > 0);
                        addressString[--offset] = '.';
                        number = (int)(m_Address & 0xFF);
                        do
                        {
                            addressString[--offset] = (char)('0' + number % 10);
                            number = number / 10;
                        } while (number > 0);
                        m_ToString = new string(addressString, offset, MaxSize - offset);
                    }
                }
            }
            return m_ToString;
        }

        private static long HostToNetworkOrder(long host)
        {
#if BIGENDIAN
            return host; 
#else
            return (((long)HostToNetworkOrder((int)host) & 0xFFFFFFFF) << 32)
                    | ((long)HostToNetworkOrder((int)(host >> 32)) & 0xFFFFFFFF);
#endif
        }
        private static int HostToNetworkOrder(int host)
        {
#if BIGENDIAN
            return host;
#else 
            return (((int)HostToNetworkOrder((short)host) & 0xFFFF) << 16)
                    | ((int)HostToNetworkOrder((short)(host >> 16)) & 0xFFFF);
#endif
        }
        private static short HostToNetworkOrder(short host)
        {
#if BIGENDIAN
            return host;
#else
            return (short)((((int)host & 0xFF) << 8) | (int)((host >> 8) & 0xFF));
#endif
        }
        private static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }
        private static int NetworkToHostOrder(int network)
        {
            return HostToNetworkOrder(network);
        }
        private static short NetworkToHostOrder(short network)
        {
            return HostToNetworkOrder(network);
        }

        private static bool IsLoopback(IPAddress address)
        {
            if (address.m_Family == AddressFamily.InterNetworkV6)
            {
                //
                // Do Equals test for IPv6 addresses
                //
                return address.Equals(IPv6Loopback);
            }
            else
            {
                return ((address.m_Address & IPAddress.LoopbackMask) == (IPAddress.Loopback.m_Address & IPAddress.LoopbackMask));
            }
        }

        internal bool IsBroadcast
        {
            get
            {
                if (m_Family == AddressFamily.InterNetworkV6)
                {
                    //
                    // No such thing as a broadcast address for IPv6 
                    // 
                    return false;
                }
                else
                {
                    return m_Address == Broadcast.m_Address;
                }
            }
        }

        /// <devdoc> 
        ///    <para>
        ///       V.Next: Determines if an address is an IPv6 Multicast address 
        ///    </para>
        /// </devdoc>
        private bool IsIPv6Multicast
        {
            get
            {
                return (m_Family == AddressFamily.InterNetworkV6) &&
                    ((this.m_Numbers[0] & 0xFF00) == 0xFF00);
            }

        }

        /*
        // Consider removing.
        /// <devdoc> 
        ///    <para>
        ///       V.Next: Determines if an address is an IPv6 Multicast Node local address 
        ///    </para> 
        /// </devdoc>
        internal bool IsIPv6MulticastNodeLocal() { 
            return ( IsIPv6Multicast ) &&
                   ( ( this.m_Numbers[0] & 0x000F ) == 1 );
 
        } 
        */

        /// <devdoc> 
        ///    <para>
        ///       V.Next: Determines if an address is an IPv6 Multicast Link local address 
        ///    </para>
        /// </devdoc>
        /*
        // Consider removing. 
        internal bool IsIPv6MulticastLinkLocal() {
            return ( IsIPv6Multicast ) && 
                   ( ( this.m_Numbers[0] & 0x000F ) == 0x0002 ); 
 
        } 
        */

        /*
        // Consider removing. 
        /// <devdoc>
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv6 Multicast Site local address 
        ///    </para>
        /// </devdoc> 
        internal bool IsIPv6MulticastSiteLocal() {
            return ( IsIPv6Multicast ) &&
                   ( ( this.m_Numbers[0] & 0x000F ) == 0x0005 );
  
        }
        */

        /// <devdoc>
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv6 Multicast Org local address
        ///    </para>
        /// </devdoc>
        /* 
        // Consider removing.
        internal bool IsIPv6MulticastOrgLocal() { 
            return ( IsIPv6Multicast ) && 
                   ( ( this.m_Numbers[0] & 0x000F ) == 0x0008 );
  
        }
        */

        /* 
        // Consider removing.
        /// <devdoc> 
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv6 Multicast Global address
        ///    </para> 
        /// </devdoc>
        internal bool IsIPv6MulticastGlobal() {
            return ( IsIPv6Multicast ) &&
                   ( ( this.m_Numbers[0] & 0x000F ) == 0x000E ); 
 
        } 
        */

        /// <devdoc> 
        ///    <para>
        ///       V.Next: Determines if an address is an IPv6 Link Local address
        ///    </para>
        /// </devdoc> 
        private bool IsIPv6LinkLocal
        {
            get
            {
                return (m_Family == AddressFamily.InterNetworkV6) &&
                   ((this.m_Numbers[0] & 0xFFC0) == 0xFE80);
            }
        }

        /// <devdoc>
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv6 Site Local address
        ///    </para> 
        /// </devdoc> 
        private bool IsIPv6SiteLocal
        {
            get
            {
                return (m_Family == AddressFamily.InterNetworkV6) &&
                   ((this.m_Numbers[0] & 0xFFC0) == 0xFEC0);
            }
        }

        /* 
        // Consider removing. 
        /// <devdoc>
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv4 Mapped Address
        ///    </para>
        /// </devdoc>
        internal bool IsIPv4Mapped() { 
            return ( m_Family == AddressFamily.InterNetworkV6 ) &&
                   ( m_Numbers[0] == 0x0000 ) && 
                   ( m_Numbers[1] == 0x0000 ) && 
                   ( m_Numbers[2] == 0x0000 ) &&
                   ( m_Numbers[3] == 0x0000 ) && 
                   ( m_Numbers[4] == 0x0000 ) &&
                   ( m_Numbers[5] == 0xffff );
        }
        */

        /* 
        // Consider removing. 
        /// <devdoc>
        ///    <para> 
        ///       V.Next: Determines if an address is an IPv4 Compatible Address
        ///    </para>
        /// </devdoc>
        internal bool IsIPv4Compatible() { 
            return ( m_Family == AddressFamily.InterNetworkV6 ) &&
                   ( m_Numbers[0] == 0x0000 ) && 
                   ( m_Numbers[1] == 0x0000 ) && 
                   ( m_Numbers[2] == 0x0000 ) &&
                   ( m_Numbers[3] == 0x0000 ) && 
                   ( m_Numbers[4] == 0x0000 ) &&
                   ( m_Numbers[5] == 0x0000 ) &&
                   !( ( m_Numbers[6] == 0x0000 )              &&
                      ( ( m_Numbers[7] & 0xFF00 ) == 0x0000 ) && 
                      ( ( ( m_Numbers[7] & 0x00FF ) == 0x0000 ) || ( m_Numbers[7] & 0x00FF ) == 0x0001 ) );
        } 
        */

        internal bool Equals(object comparand, bool compareScopeId)
        {
            if (!(comparand is IPAddress))
            {
                return false;
            }
            // 
            // Compare families before address representations
            // 
            if (m_Family != ((IPAddress)comparand).m_Family)
            {
                return false;
            }
            if (m_Family == AddressFamily.InterNetworkV6)
            {
                //
                // For IPv6 addresses, we must compare the full 128bit
                // representation. 
                //
                for (int i = 0; i < NumberOfLabels; i++)
                {
                    if (((IPAddress)comparand).m_Numbers[i] != this.m_Numbers[i])
                        return false;
                }
                //
                // In addition, the scope id's must match as well
                //
                if (((IPAddress)comparand).m_ScopeId == this.m_ScopeId)
                    return true;
                else
                    return (compareScopeId ? false : true);
            }
            else
            {
                //
                // For IPv4 addresses, compare the integer representation.
                //
                return ((IPAddress)comparand).m_Address == this.m_Address;
            }
        }

        /// <devdoc>
        ///    <para> 
        ///       Compares two IP addresses.
        ///    </para>
        /// </devdoc>
        public override bool Equals(object comparand)
        {
            return Equals(comparand, true);
        }

      

        // For security, we need to be able to take an IPAddress and make a copy that's immutable and not derived.
       
    } // class IPAddress 
} // namespace System.Net

