/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

using System;
using System.ComponentModel;
using System.IO;
using UInt8 = System.Byte;

namespace SpdReaderWriterDll {
    
    /// <summary>
    /// SPD class to deal with SPD data
    /// </summary>
    public class Spd {

        /// <summary>
        /// Gets RAM type present on the device's I2C bus
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return RamType.DDR5;
            }

            if (device.DetectDdr4()) {
                return RamType.DDR4;
            }

            // Byte at offset 0x02 in SPD indicates RAM type
            try {
                return GetRamType(Eeprom.ReadByte(device, 0, 3));
            }
            catch {
                throw new Exception($"Unable to detect RAM type at {device.I2CAddress} on {device.PortName}");
            }
        }

        /// <summary>
        /// Gets RAM type from SPD data
        /// </summary>
        /// <param name="input">SPD dump</param>
        /// <returns>RAM Type</returns>
        public static RamType GetRamType(byte[] input) {

            // Byte at offset 0x02 in SPD indicates RAM type
            return input.Length >= 3 && Enum.IsDefined(typeof(RamType), (RamType)input[0x02]) ? (RamType)input[0x02] : RamType.UNKNOWN;
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="device">Device instance</param>
        /// <returns>SPD size</returns>
        public static DataLength GetSpdSize(Arduino device) {

            if (device == null) {
                throw new NullReferenceException($"Invalid device");
            }

            if (!device.IsConnected) {
                throw new IOException($"Device not connected ({device.PortName})");
            }

            if (device.DetectDdr5()) {
                return DataLength.DDR5;
            }

            if (device.DetectDdr4()) { 
                return DataLength.DDR4;
            }

            if (device.Scan().Length != 0) {
                return DataLength.MINIMUM;
            }

            return 0;            
        }

        /// <summary>
        /// Gets total EEPROM size
        /// </summary>
        /// <param name="ramType">Ram Type</param>
        /// <returns>SPD size</returns>
        public static DataLength GetSpdSize(RamType ramType) {

            switch (ramType) {
                case RamType.SDRAM:
                case RamType.DDR:
                case RamType.DDR2:
                case RamType.DDR2_FB_DIMM:
                case RamType.DDR3:
                    return DataLength.MINIMUM;
                case RamType.DDR4:
                    return DataLength.DDR4;
                case RamType.DDR5:
                    return DataLength.DDR5;
                default:
                    return DataLength.UNKNOWN;
            }
        }

        /// <summary>
        /// Validates SPD data
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns><see langword="true"/> if <paramref name="input"/> data is a valid SPD dump</returns>
        public static bool ValidateSpd(byte[] input) {

            switch (input.Length) {
                case (int)DataLength.DDR5 when GetRamType(input) == RamType.DDR5:
                case (int)DataLength.DDR4 when GetRamType(input) == RamType.DDR4:
                    return true;
                case (int)DataLength.MINIMUM:
                    return GetRamType(input) == RamType.DDR3 || 
                           GetRamType(input) == RamType.DDR2 || 
                           GetRamType(input) == RamType.DDR  ||
                           GetRamType(input) == RamType.SDRAM;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets manufacturer from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Manufacturer's name</returns>
        public static string GetModuleManufacturer(byte[] input) {
            UInt16 manufacturerId = 0;

            switch (GetRamType(input)) {
                case RamType.DDR5:
                    manufacturerId = (UInt16)((UInt16)(input[0x200] << 8 | input[0x201]) & 0x7FFF);
                    break;
                case RamType.DDR4:
                    manufacturerId = (UInt16)((input[0x140] << 8 | input[0x141]) & 0x7FFF);
                    break;
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    manufacturerId = (UInt16)((input[0x75] << 8 | input[0x76]) & 0x7FFF);
                    break;

                // Vendor ID location for DDR2 and older RAM SPDs
                default:
                    int vendorIdOffsetStart = 0x40;
                    int vendorIdOffsetEnd   = 0x47;

                    const byte continuationCode = 0x7F;

                    byte[] manufacturerIdArray = new byte[vendorIdOffsetEnd - vendorIdOffsetStart];

                    for (UInt8 i = 0; i < manufacturerIdArray.Length; i++) {
                        manufacturerIdArray[i] = input[vendorIdOffsetStart + i];

                        if (manufacturerIdArray[i] == continuationCode) {
                            // Set manufacturer's code LSB
                            manufacturerId = (UInt16)((i + 1) << 8);
                        }
                        else {
                            // Set manufacturer's code MSB
                            manufacturerId |= manufacturerIdArray[i];
                            break;
                        }
                    }

                    break;
            }

            // Manufacturer’s identification code table
            UInt16[] manufacturerCodeTable = {
                0x01, 0x02, 0x83, 0x04, 0x85, 0x86, 0x07, 0x08, 0x89, 0x8A, 0x0B, 0x8C, 0x0D, 0x0E, 0x8F, 0x10,
                0x91, 0x92, 0x13, 0x94, 0x15, 0x16, 0x97, 0x98, 0x19, 0x1A, 0x9B, 0x1C, 0x9D, 0x9E, 0x1F, 0x20,
                0xA1, 0xA2, 0x23, 0xA4, 0x25, 0x26, 0xA7, 0xA8, 0x29, 0x2A, 0xAB, 0x2C, 0xAD, 0xAE, 0x2F, 0xB0,
                0x31, 0x32, 0xB3, 0x34, 0xB5, 0xB6, 0x37, 0x38, 0xB9, 0xBA, 0x3B, 0xBC, 0x3D, 0x3E, 0xBF, 0x40,
                0xC1, 0xC2, 0x43, 0xC4, 0x45, 0x46, 0xC7, 0xC8, 0x49, 0x4A, 0xCB, 0x4C, 0xCD, 0xCE, 0x4F, 0xD0,
                0x51, 0x52, 0xD3, 0x54, 0xD5, 0xD6, 0x57, 0x58, 0xD9, 0xDA, 0x5B, 0xDC, 0x5D, 0x5E, 0xDF, 0xE0,
                0x61, 0x62, 0xE3, 0x64, 0xE5, 0xE6, 0x67, 0x68, 0xE9, 0xEA, 0x6B, 0xEC, 0x6D, 0x6E, 0xEF, 0x70,
                0xF1, 0xF2, 0x73, 0xF4, 0x75, 0x76, 0xF7, 0xF8, 0x79, 0x7A, 0xFB, 0x7C, 0xFD, 0xFE };

            // Manufacturer’s names table
            string[][] manufacturerNameTable = {
                // Continuation code 0x00
                new[] {
                    "AMD", "AMI", "Fairchild", "Fujitsu", "GTE", "Harris", "Hitachi", "Inmos", "Intel", "I.T.T.", "Intersil", "Monolithic Memories", "Mostek",
                    "Freescale (Motorola)", "National", "NEC", "RCA", "Raytheon", "Conexant (Rockwell)", "Seeq", "NXP (Philips)", "Synertek",
                    "Texas Instruments", "Kioxia Corporation", "Xicor", "Zilog", "Eurotechnique", "Mitsubishi", "Lucent (AT&T)", "Exel", "Atmel",
                    "STMicroelectronics", "Lattice Semi.", "NCR", "Wafer Scale Integration", "IBM", "Tristar", "Visic", "Intl. CMOS Technology", "SSSI",
                    "Microchip Technology", "Ricoh Ltd", "VLSI", "Micron Technology", "SK Hynix", "OKI Semiconductor", "ACTEL", "Sharp", "Catalyst",
                    "Panasonic", "IDT", "Cypress", "DEC", "LSI Logic", "Zarlink (Plessey)", "UTMC", "Thinking Machine", "Thomson CSF",
                    "Integrated CMOS (Vertex)", "Honeywell", "Tektronix", "Oracle Corporation", "Silicon Storage Technology", "ProMos/Mosel Vitelic",
                    "Infineon (Siemens)", "Macronix", "Xerox", "Plus Logic", "Western Digital Technologies Inc", "Elan Circuit Tech.", "European Silicon Str.",
                    "Apple Computer", "Xilinx", "Compaq", "Protocol Engines", "SCI", "Seiko Instruments", "Samsung", "I3 Design System", "Klic",
                    "Crosspoint Solutions", "Alliance Memory Inc", "Tandem", "Hewlett-Packard", "Integrated Silicon Solutions", "Brooktree", "New Media",
                    "MHS Electronic", "Performance Semi.", "Winbond Electronic", "Kawasaki Steel", "Bright Micro", "TECMAR", "Exar", "PCMCIA",
                    "LG Semi (Goldstar)", "Northern Telecom", "Sanyo", "Array Microsystems", "Crystal Semiconductor", "Analog Devices", "PMC-Sierra", "Asparix",
                    "Convex Computer", "Quality Semiconductor", "Nimbus Technology", "Transwitch", "Micronas (ITT Intermetall)", "Cannon", "Altera", "NEXCOM",
                    "Qualcomm", "Sony", "Cray Research", "AMS(Austria Micro)", "Vitesse", "Aster Electronics", "Bay Networks (Synoptic)", "Zentrum/ZMD", "TRW",
                    "Thesys", "Solbourne Computer", "Allied-Signal", "Dialog Semiconductor", "Media Vision", "Numonyx Corporation"
                },
                // Continuation code 0x01
                new[] {
                    "Cirrus Logic", "National Instruments", "ILC Data Device", "Alcatel Mietec", "Micro Linear", "Univ. of NC", "JTAG Technologies",
                    "BAE Systems (Loral)", "Nchip", "Galileo Tech", "Bestlink Systems", "Graychip", "GENNUM", "VideoLogic", "Robert Bosch", "Chip Express",
                    "DATARAM", "United Microelectronics Corp", "TCSI", "Smart Modular", "Hughes Aircraft", "Lanstar Semiconductor", "Qlogic", "Kingston",
                    "Music Semi", "Ericsson Components", "SpaSE", "Eon Silicon Devices", "Integrated Silicon Solution (ISSI)", "DoD", "Integ. Memories Tech.",
                    "Corollary Inc", "Dallas Semiconductor", "Omnivision", "EIV(Switzerland)", "Novatel Wireless", "Zarlink (Mitel)", "Clearpoint", "Cabletron",
                    "STEC (Silicon Tech)", "Vanguard", "Hagiwara Sys-Com", "Vantis", "Celestica", "Century", "Hal Computers", "Rohm Company Ltd",
                    "Juniper Networks", "Libit Signal Processing", "Mushkin Enhanced Memory", "Tundra Semiconductor", "Adaptec Inc", "LightSpeed Semi.",
                    "ZSP Corp", "AMIC Technology", "Adobe Systems", "Dynachip", "PNY Technologies Inc", "Newport Digital", "MMC Networks", "T Square",
                    "Seiko Epson", "Broadcom", "Viking Components", "V3 Semiconductor", "Flextronics (Orbit Semiconductor)", "Suwa Electronics", "Transmeta",
                    "Micron CMS", "American Computer & Digital Components Inc", "Enhance 3000 Inc", "Tower Semiconductor", "CPU Design", "Price Point",
                    "Maxim Integrated Product", "Tellabs", "Centaur Technology", "Unigen Corporation", "Transcend Information", "Memory Card Technology",
                    "CKD Corporation Ltd", "Capital Instruments Inc", "Aica Kogyo Ltd", "Linvex Technology", "MSC Vertriebs GmbH", "AKM Company Ltd",
                    "Dynamem Inc", "NERA ASA", "GSI Technology", "Dane-Elec (C Memory)", "Acorn Computers", "Lara Technology", "Oak Technology Inc",
                    "Itec Memory", "Tanisys Technology", "Truevision", "Wintec Industries", "Super PC Memory", "MGV Memory", "Galvantech", "Gadzoox Networks",
                    "Multi Dimensional Cons.", "GateField", "Integrated Memory System", "Triscend", "XaQti", "Goldenram", "Clear Logic",
                    "Cimaron Communications", "Nippon Steel Semi. Corp", "Advantage Memory", "AMCC", "LeCroy", "Yamaha Corporation", "Digital Microwave",
                    "NetLogic Microsystems", "MIMOS Semiconductor", "Advanced Fibre", "BF Goodrich Data.", "Epigram", "Acbel Polytech Inc", "Apacer Technology",
                    "Admor Memory", "FOXCONN", "Quadratics Superconductor", "3COM"
                },
                // Continuation code 0x02
                new[] {
                    "Camintonn Corporation", "ISOA Incorporated", "Agate Semiconductor", "ADMtek Incorporated", "HYPERTEC", "Adhoc Technologies",
                    "MOSAID Technologies", "Ardent Technologies", "Switchcore", "Cisco Systems Inc", "Allayer Technologies", "WorkX AG (Wichman)",
                    "Oasis Semiconductor", "Novanet Semiconductor", "E-M Solutions", "Power General", "Advanced Hardware Arch.", "Inova Semiconductors GmbH",
                    "Telocity", "Delkin Devices", "Symagery Microsystems", "C-Port Corporation", "SiberCore Technologies", "Southland Microsystems",
                    "Malleable Technologies", "Kendin Communications", "Great Technology Microcomputer", "Sanmina Corporation", "HADCO Corporation", "Corsair",
                    "Actrans System Inc", "ALPHA Technologies", "Silicon Laboratories Inc (Cygnal)", "Artesyn Technologies", "Align Manufacturing",
                    "Peregrine Semiconductor", "Chameleon Systems", "Aplus Flash Technology", "MIPS Technologies", "Chrysalis ITS", "ADTEC Corporation",
                    "Kentron Technologies", "Win Technologies", "Tezzaron Semiconductor", "Extreme Packet Devices", "RF Micro Devices", "Siemens AG",
                    "Sarnoff Corporation", "Itautec SA", "Radiata Inc", "Benchmark Elect. (AVEX)", "Legend", "SpecTek Incorporated", "Hi/fn",
                    "Enikia Incorporated", "SwitchOn Networks", "AANetcom Incorporated", "Micro Memory Bank", "ESS Technology", "Virata Corporation",
                    "Excess Bandwidth", "West Bay Semiconductor", "DSP Group", "Newport Communications", "Chip2Chip Incorporated", "Phobos Corporation",
                    "Intellitech Corporation", "Nordic VLSI ASA", "Ishoni Networks", "Silicon Spice", "Alchemy Semiconductor", "Agilent Technologies",
                    "Centillium Communications", "W.L. Gore", "HanBit Electronics", "GlobeSpan", "Element 14", "Pycon", "Saifun Semiconductors",
                    "Sibyte Incorporated", "MetaLink Technologies", "Feiya Technology", "I & C Technology", "Shikatronics", "Elektrobit", "Megic", "Com-Tier",
                    "Malaysia Micro Solutions", "Hyperchip", "Gemstone Communications", "Anadigm (Anadyne)", "3ParData", "Mellanox Technologies",
                    "Tenx Technologies", "Helix AG", "Domosys", "Skyup Technology", "HiNT Corporation", "Chiaro", "MDT Technologies GmbH",
                    "Exbit Technology A/S", "Integrated Technology Express", "AVED Memory", "Legerity", "Jasmine Networks", "Caspian Networks", "nCUBE",
                    "Silicon Access Networks", "FDK Corporation", "High Bandwidth Access", "MultiLink Technology", "BRECIS", "World Wide Packets", "APW",
                    "Chicory Systems", "Xstream Logic", "Fast-Chip", "Zucotto Wireless", "Realchip", "Galaxy Power", "eSilicon", "Morphics Technology",
                    "Accelerant Networks", "Silicon Wave", "SandCraft", "Elpida"
                },
                // Continuation code 0x03
                new[] {
                    "Solectron", "Optosys Technologies", "Buffalo (Formerly Melco)", "TriMedia Technologies", "Cyan Technologies", "Global Locate", "Optillion",
                    "Terago Communications", "Ikanos Communications", "Princeton Technology", "Nanya Technology", "Elite Flash Storage", "Mysticom",
                    "LightSand Communications", "ATI Technologies", "Agere Systems", "NeoMagic", "AuroraNetics", "Golden Empire", "Mushkin",
                    "Tioga Technologies", "Netlist", "TeraLogic", "Cicada Semiconductor", "Centon Electronics", "Tyco Electronics", "Magis Works", "Zettacom",
                    "Cogency Semiconductor", "Chipcon AS", "Aspex Technology", "F5 Networks", "Programmable Silicon Solutions", "ChipWrights", "Acorn Networks",
                    "Quicklogic", "Kingmax Semiconductor", "BOPS", "Flasys", "BitBlitz Communications", "eMemory Technology", "Procket Networks", "Purple Ray",
                    "Trebia Networks", "Delta Electronics", "Onex Communications", "Ample Communications", "Memory Experts Intl", "Astute Networks",
                    "Azanda Network Devices", "Dibcom", "Tekmos", "API NetWorks", "Bay Microsystems", "Firecron Ltd", "Resonext Communications",
                    "Tachys Technologies", "Equator Technology", "Concept Computer", "SILCOM", "3Dlabs", "c’t Magazine", "Sanera Systems", "Silicon Packets",
                    "Viasystems Group", "Simtek", "Semicon Devices Singapore", "Satron Handelsges", "Improv Systems", "INDUSYS GmbH", "Corrent",
                    "Infrant Technologies", "Ritek Corp", "empowerTel Networks", "Hypertec", "Cavium Networks", "PLX Technology", "Massana Design",
                    "Intrinsity", "Valence Semiconductor", "Terawave Communications", "IceFyre Semiconductor", "Primarion", "Picochip Designs Ltd",
                    "Silverback Systems", "Jade Star Technologies", "Pijnenburg Securealink", "takeMS - Ultron AG", "Cambridge Silicon Radio", "Swissbit",
                    "Nazomi Communications", "eWave System", "Rockwell Collins", "Picocel Co Ltd (Paion)", "Alphamosaic Ltd", "Sandburst", "SiCon Video",
                    "NanoAmp Solutions", "Ericsson Technology", "PrairieComm", "Mitac International", "Layer N Networks", "MtekVision (Atsana)",
                    "Allegro Networks", "Marvell Semiconductors", "Netergy Microelectronic", "NVIDIA", "Internet Machines", "Memorysolution GmbH",
                    "Litchfield Communication", "Accton Technology", "Teradiant Networks", "Scaleo Chip", "Cortina Systems", "RAM Components", "Raqia Networks",
                    "ClearSpeed", "Matsushita Battery", "Xelerated", "SimpleTech", "Utron Technology", "Astec International", "AVM gmbH",
                    "Redux Communications", "Dot Hill Systems", "TeraChip"
                },
                // Continuation code 0x04
                new[] {
                    "T-RAM Incorporated", "Innovics Wireless", "Teknovus", "KeyEye Communications", "Runcom Technologies", "RedSwitch", "Dotcast",
                    "Silicon Mountain Memory", "Signia Technologies", "Pixim", "Galazar Networks", "White Electronic Designs", "Patriot Scientific",
                    "Neoaxiom Corporation", "3Y Power Technology", "Scaleo Chip", "Potentia Power Systems", "C-guys Incorporated",
                    "Digital Communications Technology Inc", "Silicon-Based Technology", "Fulcrum Microsystems", "Positivo Informatica Ltd",
                    "XIOtech Corporation", "PortalPlayer", "Zhiying Software", "ParkerVision Inc", "Phonex Broadband", "Skyworks Solutions",
                    "Entropic Communications", "I’M Intelligent Memory Ltd", "Zensys A/S", "Legend Silicon Corp", "Sci-worx GmbH",
                    "SMSC (Standard Microsystems)", "Renesas Electronics", "Raza Microelectronics", "Phyworks", "MediaTek", "Non-cents Productions",
                    "US Modular", "Wintegra Ltd", "Mathstar", "StarCore", "Oplus Technologies", "Mindspeed", "Just Young Computer", "Radia Communications",
                    "OCZ", "Emuzed", "LOGIC Devices", "Inphi Corporation", "Quake Technologies", "Vixel", "SolusTek", "Kongsberg Maritime",
                    "Faraday Technology", "Altium Ltd", "Insyte", "ARM Ltd", "DigiVision", "Vativ Technologies", "Endicott Interconnect Technologies",
                    "Pericom", "Bandspeed", "LeWiz Communications", "CPU Technology", "Ramaxel Technology", "DSP Group", "Axis Communications",
                    "Legacy Electronics", "Chrontel", "Powerchip Semiconductor", "MobilEye Technologies", "Excel Semiconductor", "A-DATA Technology",
                    "VirtualDigm", "G Skill Intl", "Quanta Computer", "Yield Microelectronics", "Afa Technologies", "KINGBOX Technology Co Ltd", "Ceva",
                    "iStor Networks", "Advance Modules", "Microsoft", "Open-Silicon", "Goal Semiconductor", "ARC International", "Simmtec", "Metanoia",
                    "Key Stream", "Lowrance Electronics", "Adimos", "SiGe Semiconductor", "Fodus Communications", "Credence Systems Corp",
                    "Genesis Microchip Inc", "Vihana Inc", "WIS Technologies", "GateChange Technologies", "High Density Devices AS", "Synopsys", "Gigaram",
                    "Enigma Semiconductor Inc", "Century Micro Inc", "Icera Semiconductor", "Mediaworks Integrated Systems", "O’Neil Product Development",
                    "Supreme Top Technology Ltd", "MicroDisplay Corporation", "Team Group Inc", "Sinett Corporation", "Toshiba Corporation", "Tensilica",
                    "SiRF Technology", "Bacoc Inc", "SMaL Camera Technologies", "Thomson SC", "Airgo Networks", "Wisair Ltd", "SigmaTel", "Arkados",
                    "Compete IT gmbH Co KG", "Eudar Technology Inc", "Focus Enhancements", "Xyratex"
                },
                // Continuation code 0x05
                new[] {
                    "Specular Networks", "Patriot Memory (PDP Systems)", "U-Chip Technology Corp", "Silicon Optix", "Greenfield Networks", "CompuRAM GmbH",
                    "Stargen Inc", "NetCell Corporation", "Excalibrus Technologies Ltd", "SCM Microsystems", "Xsigo Systems Inc", "CHIPS & Systems Inc",
                    "Tier 1 Multichip Solutions", "CWRL Labs", "Teradici", "Gigaram Inc", "g2 Microsystems", "PowerFlash Semiconductor", "P.A. Semi Inc",
                    "NovaTech Solutions S.A.", "c2 Microsystems Inc", "Level5 Networks", "COS Memory AG", "Innovasic Semiconductor", "02IC Co Ltd",
                    "Tabula Inc", "Crucial Technology", "Chelsio Communications", "Solarflare Communications", "Xambala Inc", "EADS Astrium",
                    "Terra Semiconductor Inc", "Imaging Works Inc", "Astute Networks Inc", "Tzero", "Emulex", "Power-One", "Pulse~LINK Inc",
                    "Hon Hai Precision Industry", "White Rock Networks Inc", "Telegent Systems USA Inc", "Atrua Technologies Inc", "Acbel Polytech Inc",
                    "eRide Inc", "ULi Electronics Inc", "Magnum Semiconductor Inc", "neoOne Technology Inc", "Connex Technology Inc", "Stream Processors Inc",
                    "Focus Enhancements", "Telecis Wireless Inc", "uNav Microelectronics", "Tarari Inc", "Ambric Inc", "Newport Media Inc", "VMTS",
                    "Enuclia Semiconductor Inc", "Virtium Technology Inc", "Solid State System Co Ltd", "Kian Tech LLC", "Artimi",
                    "Power Quotient International", "Avago Technologies", "ADTechnology", "Sigma Designs", "SiCortex Inc", "Ventura Technology Group", "eASIC",
                    "M.H.S. SAS", "Micro Star International", "Rapport Inc", "Makway International", "Broad Reach Engineering Co",
                    "Semiconductor Mfg Intl Corp", "SiConnect", "FCI USA Inc", "Validity Sensors", "Coney Technology Co Ltd", "Spans Logic", "Neterion Inc",
                    "Qimonda", "New Japan Radio Co Ltd", "Velogix", "Montalvo Systems", "iVivity Inc", "Walton Chaintech", "AENEON", "Lorom Industrial Co Ltd",
                    "Radiospire Networks", "Sensio Technologies Inc", "Nethra Imaging", "Hexon Technology Pte Ltd", "CompuStocx (CSX)",
                    "Methode Electronics Inc", "Connect One Ltd", "Opulan Technologies", "Septentrio NV", "Goldenmars Technology Inc", "Kreton Corporation",
                    "Cochlear Ltd", "Altair Semiconductor", "NetEffect Inc", "Spansion Inc", "Taiwan Semiconductor Mfg", "Emphany Systems Inc",
                    "ApaceWave Technologies", "Mobilygen Corporation", "Tego", "Cswitch Corporation", "Haier (Beijing) IC Design Co", "MetaRAM",
                    "Axel Electronics Co Ltd", "Tilera Corporation", "Aquantia", "Vivace Semiconductor", "Redpine Signals", "Octalica",
                    "InterDigital Communications", "Avant Technology", "Asrock Inc", "Availink", "Quartics Inc", "Element CXI",
                    "Innovaciones Microelectronicas", "VeriSilicon Microelectronics", "W5 Networks"
                },
                // Continuation code 0x06
                new[] {
                    "MOVEKING", "Mavrix Technology Inc", "CellGuide Ltd", "Faraday Technology", "Diablo Technologies Inc", "Jennic", "Octasic",
                    "Molex Incorporated", "3Leaf Networks", "Bright Micron Technology", "Netxen", "NextWave Broadband Inc", "DisplayLink", "ZMOS Technology",
                    "Tec-Hill", "Multigig Inc", "Amimon", "Euphonic Technologies Inc", "BRN Phoenix", "InSilica", "Ember Corporation",
                    "Avexir Technologies Corporation", "Echelon Corporation", "Edgewater Computer Systems", "XMOS Semiconductor Ltd", "GENUSION Inc",
                    "Memory Corp NV", "SiliconBlue Technologies", "Rambus Inc", "Andes Technology Corporation", "Coronis Systems", "Achronix Semiconductor",
                    "Siano Mobile Silicon Ltd", "Semtech Corporation", "Pixelworks Inc", "Gaisler Research AB", "Teranetics", "Toppan Printing Co Ltd",
                    "Kingxcon", "Silicon Integrated Systems", "I-O Data Device Inc", "NDS Americas Inc", "Solomon Systech Limited",
                    "On Demand Microelectronics", "Amicus Wireless Inc", "SMARDTV SNC", "Comsys Communication Ltd", "Movidia Ltd", "Javad GNSS Inc",
                    "Montage Technology Group", "Trident Microsystems", "Super Talent", "Optichron Inc", "Future Waves UK Ltd", "SiBEAM Inc", "InicoreInc",
                    "Virident Systems", "M2000 Inc", "ZeroG Wireless Inc", "Gingle Technology Co Ltd", "Space Micro Inc", "Wilocity", "Novafora Inc",
                    "iKoa Corporation", "ASint Technology", "Ramtron", "Plato Networks Inc", "IPtronics AS", "Infinite-Memories", "Parade Technologies Inc",
                    "Dune Networks", "GigaDevice Semiconductor", "Modu Ltd", "CEITEC", "Northrop Grumman", "XRONET Corporation", "Sicon Semiconductor AB",
                    "Atla Electronics Co Ltd", "TOPRAM Technology", "Silego Technology Inc", "Kinglife", "Ability Industries Ltd",
                    "Silicon Power Computer & Communications", "Augusta Technology Inc", "Nantronics Semiconductors", "Hilscher Gesellschaft", "Quixant Ltd",
                    "Percello Ltd", "NextIO Inc", "Scanimetrics Inc", "FS-Semi Company Ltd", "Infinera Corporation", "SandForce Inc", "Lexar Media",
                    "Teradyne Inc", "Memory Exchange Corp", "Suzhou Smartek Electronics", "Avantium Corporation", "ATP Electronics Inc",
                    "Valens Semiconductor Ltd", "Agate Logic Inc", "Netronome", "Zenverge Inc", "N-trig Ltd", "SanMax Technologies Inc",
                    "Contour Semiconductor Inc", "TwinMOS", "Silicon Systems Inc", "V-Color Technology Inc", "Certicom Corporation", "JSC ICC Milandr",
                    "PhotoFast Global Inc", "InnoDisk Corporation", "Muscle Power", "Energy Micro", "Innofidei", "CopperGate Communications",
                    "Holtek Semiconductor Inc", "Myson Century Inc", "FIDELIX", "Red Digital Cinema", "Densbits Technology", "Zempro", "MoSys", "Provigent",
                    "Triad Semiconductor Inc"
                },
                // Continuation code 0x07
                new[] {
                    "Siklu Communication Ltd", "A Force Manufacturing Ltd", "Strontium", "ALi Corp (Abilis Systems)", "Siglead Inc", "Ubicom Inc",
                    "Unifosa Corporation", "Stretch Inc", "Lantiq Deutschland GmbH", "Visipro.", "EKMemory", "Microelectronics Institute ZTE", "u-blox AG",
                    "Carry Technology Co Ltd", "Nokia", "King Tiger Technology", "Sierra Wireless", "HT Micron", "Albatron Technology Co Ltd",
                    "Leica Geosystems AG", "BroadLight", "AEXEA", "ClariPhy Communications Inc", "Green Plug", "Design Art Networks",
                    "Mach Xtreme Technology Ltd", "ATO Solutions Co Ltd", "Ramsta", "Greenliant Systems Ltd", "Teikon", "Antec Hadron", "NavCom Technology Inc",
                    "Shanghai Fudan Microelectronics", "Calxeda Inc", "JSC EDC Electronics", "Kandit Technology Co Ltd", "Ramos Technology",
                    "Goldenmars Technology", "XeL Technology Inc", "Newzone Corporation", "ShenZhen MercyPower Tech", "Nanjing Yihuo Technology",
                    "Nethra Imaging Inc", "SiTel Semiconductor BV", "SolidGear Corporation", "Topower Computer Ind Co Ltd", "Wilocity", "Profichip GmbH",
                    "Gerad Technologies", "Ritek Corporation", "Gomos Technology Limited", "Memoright Corporation", "D-Broad Inc", "HiSilicon Technologies",
                    "Syndiant Inc.", "Enverv Inc", "Cognex", "Xinnova Technology Inc", "Ultron AG", "Concord Idea Corporation", "AIM Corporation",
                    "Lifetime Memory Products", "Ramsway", "Recore Systems B.V.", "Haotian Jinshibo Science Tech", "Being Advanced Memory",
                    "Adesto Technologies", "Giantec Semiconductor Inc", "HMD Electronics AG", "Gloway International (HK)", "Kingcore",
                    "Anucell Technology Holding", "Accord Software & Systems Pvt. Ltd", "Active-Semi Inc", "Denso Corporation", "TLSI Inc", "Qidan", "Mustang",
                    "Orca Systems", "Passif Semiconductor", "GigaDevice Semiconductor (Beijing) Inc", "Memphis Electronic", "Beckhoff Automation GmbH",
                    "Harmony Semiconductor Corp", "Air Computers SRL", "TMT Memory", "Eorex Corporation", "Xingtera", "Netsol", "Bestdon Technology Co Ltd",
                    "Baysand Inc", "Uroad Technology Co Ltd", "Wilk Elektronik S.A.", "AAI", "Harman", "Berg Microelectronics Inc", "ASSIA Inc",
                    "Visiontek Products LLC", "OCMEMORY", "Welink Solution Inc", "Shark Gaming", "Avalanche Technology", "R&D Center ELVEES OJSC",
                    "KingboMars Technology Co Ltd", "High Bridge Solutions Industria Eletronica", "Transcend Technology Co Ltd", "Everspin Technologies",
                    "Hon-Hai Precision", "Smart Storage Systems", "Toumaz Group", "Zentel Electronics Corporation", "Panram International Corporation",
                    "Silicon Space Technology", "LITE-ON IT Corporation", "Inuitive", "HMicro", "BittWare Inc", "GLOBALFOUNDRIES", "ACPI Digital Co Ltd",
                    "Annapurna Labs", "AcSiP Technology Corporation", "Idea! Electronic Systems", "Gowe Technology Co Ltd", "Hermes Testing Solutions Inc",
                    "Positivo BGH", "Intelligence Silicon Technology"
                },
                // Continuation code 0x08
                new[] {
                    "3D PLUS", "Diehl Aerospace", "Fairchild", "Mercury Systems", "Sonics Inc", "Emerson Automation Solutions",
                    "Shenzhen Jinge Information Co Ltd", "SCWW", "Silicon Motion Inc", "Anurag", "King Kong", "FROM30 Co Ltd", "Gowin Semiconductor Corp",
                    "Fremont Micro Devices Ltd", "Ericsson Modems", "Exelis", "Satixfy Ltd", "Galaxy Microsystems Ltd", "Gloway International Co Ltd", "Lab",
                    "Smart Energy Instruments", "Approved Memory Corporation", "Axell Corporation", "Essencore Limited", "Phytium",
                    "Xi’an UniIC Semiconductors Co Ltd", "Ambiq Micro", "eveRAM Technology Inc", "Infomax", "Butterfly Network Inc",
                    "Shenzhen City Gcai Electronics", "Stack Devices Corporation", "ADK Media Group", "TSP Global Co Ltd", "HighX",
                    "Shenzhen Elicks Technology", "XinKai/Silicon Kaiser", "Google Inc", "Dasima International Development", "Leahkinn Technology Limited",
                    "HIMA Paul Hildebrandt GmbH Co KG", "Keysight Technologies", "Techcomp International (Fastable)", "Ancore Technology Corporation",
                    "Nuvoton", "Korea Uhbele International Group Ltd", "Ikegami Tsushinki Co Ltd", "RelChip Inc", "Baikal Electronics", "Nemostech Inc",
                    "Memorysolution GmbH", "Silicon Integrated Systems Corporation", "Xiede", "BRC", "Flash Chi", "Jone", "GCT Semiconductor Inc",
                    "Hong Kong Zetta Device Technology", "Unimemory Technology(s) Pte Ltd", "Cuso", "Kuso", "Uniquify Inc", "Skymedi Corporation",
                    "Core Chance Co Ltd", "Tekism Co Ltd", "Seagate Technology PLC", "Hong Kong Gaia Group Co Limited", "Gigacom Semiconductor LLC",
                    "V2 Technologies", "TLi", "Neotion", "Lenovo", "Shenzhen Zhongteng Electronic Corp Ltd", "Compound Photonics", "in2H2 inc",
                    "Shenzhen Pango Microsystems Co Ltd", "Vasekey", "Cal-Comp Industria de Semicondutores", "Eyenix Co Ltd", "Heoriady",
                    "Accelerated Memory Production Inc", "INVECAS Inc", "AP Memory", "Douqi Technology", "Etron Technology Inc", "Indie Semiconductor",
                    "Socionext Inc", "HGST", "EVGA", "Audience Inc", "EpicGear", "Vitesse Enterprise Co", "Foxtronn International Corporation", "Bretelon Inc",
                    "Graphcore", "Eoplex Inc", "MaxLinear Inc", "ETA Devices", "LOKI", "IMS Electronics Co Ltd", "Dosilicon Co Ltd", "Dolphin Integration",
                    "Shenzhen Mic Electronics Technolog", "Boya Microelectronics Inc", "Geniachip (Roche)", "Axign", "Kingred Electronic Technology Ltd",
                    "Chao Yue Zhuo Computer Business Dept.", "Guangzhou Si Nuo Electronic Technology.", "Crocus Technology Inc", "Creative Chips GmbH",
                    "GE Aviation Systems LLC.", "Asgard", "Good Wealth Technology Ltd", "TriCor Technologies", "Nova-Systems GmbH", "JUHOR",
                    "Zhuhai Douke Commerce Co Ltd", "DSL Memory", "Anvo-Systems Dresden GmbH", "Realtek", "AltoBeam", "Wave Computing",
                    "Beijing TrustNet Technology Co Ltd", "Innovium Inc", "Starsway Technology Limited"
                },
                // Continuation code 0x09
                new[] {
                    "Weltronics Co LTD", "VMware Inc", "Hewlett Packard Enterprise", "INTENSO", "Puya Semiconductor", "MEMORFI", "MSC Technologies GmbH",
                    "Txrui", "SiFive Inc", "Spreadtrum Communications", "XTX Technology Limited", "UMAX Technology", "Shenzhen Yong Sheng Technology",
                    "SNOAMOO (Shenzhen Kai Zhuo Yue)", "Daten Tecnologia LTDA", "Shenzhen XinRuiYan Electronics", "Eta Compute", "Energous",
                    "Raspberry Pi Trading Ltd", "Shenzhen Chixingzhe Tech Co Ltd", "Silicon Mobility", "IQ-Analog Corporation", "Uhnder Inc", "Impinj",
                    "DEPO Computers", "Nespeed Sysems", "Yangtze Memory Technologies Co Ltd", "MemxPro Inc", "Tammuz Co Ltd", "Allwinner Technology",
                    "Shenzhen City Futian District Qing Xuan Tong Computer Trading Firm", "XMC", "Teclast", "Maxsun", "Haiguang Integrated Circuit Design",
                    "RamCENTER Technology", "Phison Electronics Corporation", "Guizhou Huaxintong Semi-Conductor", "Network Intelligence",
                    "Continental Technology (Holdings)", "Guangzhou Huayan Suning Electronic", "Guangzhou Zhouji Electronic Co Ltd",
                    "Shenzhen Giant Hui Kang Tech Co Ltd", "Shenzhen Yilong Innovative Co Ltd", "Neo Forza", "Lyontek Inc",
                    "Shanghai Kuxin Microelectronics Ltd", "Shenzhen Larix Technology Co Ltd", "Qbit Semiconductor Ltd", "Insignis Technology Corporation",
                    "Lanson Memory Co Ltd", "Shenzhen Superway Electronics Co Ltd", "Canaan-Creative Co Ltd", "Black Diamond Memory",
                    "Shenzhen City Parker Baking Electronics", "Shenzhen Baihong Technology Co Ltd", "GEO Semiconductors", "OCPC", "Artery Technology Co Ltd",
                    "Jinyu", "ShenzhenYing Chi Technology Development", "Shenzhen Pengcheng Xin Technology", "Pegasus Semiconductor (Shanghai) Co",
                    "Mythic Inc", "Elmos Semiconductor AG", "Kllisre", "Shenzhen Winconway Technology", "Shenzhen Xingmem Technology Corp",
                    "Gold Key Technology Co Ltd", "Habana Labs Ltd", "Hoodisk Electronics Co Ltd", "SemsoTai (SZ) Technology Co Ltd", "OM Nanotech Pvt. Ltd",
                    "Shenzhen Zhifeng Weiye Technology", "Xinshirui (Shenzhen) Electronics Co", "Guangzhou Zhong Hao Tian Electronic",
                    "Shenzhen Longsys Electronics Co Ltd", "Deciso B.V.", "Puya Semiconductor (Shenzhen)", "Shenzhen Veineda Technology Co Ltd", "Antec Memory",
                    "Cortus SAS", "Dust Leopard", "MyWo AS", "J&A Information Inc", "Shenzhen JIEPEI Technology Co Ltd", "Heidelberg University",
                    "Flexxon PTE Ltd", "Wiliot", "Raysun Electronics International Ltd", "Aquarius Production Company LLC", "MACNICA DHW LTDA", "Intelimem",
                    "Zbit Semiconductor Inc", "Shenzhen Technology Co Ltd", "Signalchip", "Shenzen Recadata Storage Technology", "Hyundai Technology",
                    "Shanghai Fudi Investment Development", "Aixi Technology", "Tecon MT", "Onda Electric Co Ltd", "Jinshen",
                    "Kimtigo Semiconductor (HK) Limited", "IIT Madras", "Shenshan (Shenzhen) Electronic", "Hefei Core Storage Electronic Limited",
                    "Colorful Technology Ltd", "Visenta (Xiamen) Technology Co Ltd", "Roa Logic BV", "NSITEXE Inc", "Hong Kong Hyunion Electronics",
                    "ASK Technology Group Limited", "GIGA-BYTE Technology Co Ltd", "Terabyte Co Ltd", "Hyundai Inc", "EXCELERAM", "PsiKick",
                    "Netac Technology Co Ltd", "PCCOOLER", "Jiangsu Huacun Electronic Technology", "Shenzhen Micro Innovation Industry",
                    "Beijing Tongfang Microelectronics Co", "XZN Storage Technology", "ChipCraft Sp. z.o.o.", "ALLFLASH Technology Limited"
                },
                // Continuation code 0x0A
                new[] {
                    "Foerd Technology Co Ltd", "KingSpec", "Codasip GmbH", "SL Link Co Ltd", "Shenzhen Kefu Technology Co Limited",
                    "Shenzhen ZST Electronics Technology", "Kyokuto Electronic Inc", "Warrior Technology", "TRINAMIC Motion Control GmbH & Co",
                    "PixelDisplay Inc", "Shenzhen Futian District Bo Yueda Elec", "Richtek Power", "Shenzhen LianTeng Electronics Co Ltd", "AITC Memory",
                    "UNIC Memory Technology Co Ltd", "Shenzhen Huafeng Science Technology", "CXMT", "Guangzhou Xinyi Heng Computer Trading Firm",
                    "SambaNova Systems", "V-GEN", "Jump Trading", "Ampere Computing", "Shenzhen Zhongshi Technology Co Ltd",
                    "Shenzhen Zhongtian Bozhong Technology", "Tri-Tech International", "Silicon Intergrated Systems Corporation",
                    "Shenzhen HongDingChen Information", "Plexton Holdings Limited", "AMS (Jiangsu Advanced Memory Semi)",
                    "Wuhan Jing Tian Interconnected Tech Co", "Axia Memory Technology", "Chipset Technology Holding Limited",
                    "Shenzhen Xinshida Technology Co Ltd", "Shenzhen Chuangshifeida Technology", "Guangzhou MiaoYuanJi Technology", "ADVAN Inc",
                    "Shenzhen Qianhai Weishengda Electronic Commerce Company Ltd", "Guangzhou Guang Xie Cheng Trading", "StarRam International Co Ltd",
                    "Shen Zhen XinShenHua Tech Co Ltd", "UltraMemory Inc", "New Coastline Global Tech Industry Co", "Sinker", "Diamond", "PUSKILL",
                    "Guangzhou Hao Jia Ye Technology Co", "Ming Xin Limited", "Barefoot Networks", "Biwin Semiconductor (HK) Co Ltd", "UD INFO Corporation",
                    "Trek Technology (S) PTE Ltd", "Xiamen Kingblaze Technology Co Ltd", "Shenzhen Lomica Technology Co Ltd", "Nuclei System Technology Co Ltd",
                    "Wuhan Xun Zhan Electronic Technology", "Shenzhen Ingacom Semiconductor Ltd", "Zotac Technology Ltd", "Foxline",
                    "Shenzhen Farasia Science Technology", "Efinix Inc", "Hua Nan San Xian Technology Co Ltd", "Goldtech Electronics Co Ltd",
                    "Shanghai Han Rong Microelectronics Co", "Shenzhen Zhongguang Yunhe Trading", "Smart Shine(QingDao) Microelectronics",
                    "Thermaltake Technology Co Ltd", "Shenzhen O’Yang Maile Technology Ltd", "UPMEM", "Chun Well Technology Holding Limited", "Astera Labs Inc",
                    "Winconway", "Advantech Co Ltd", "Chengdu Fengcai Electronic Technology", "The Boeing Company", "Blaize Inc", "Ramonster Technology Co Ltd",
                    "Wuhan Naonongmai Technology Co Ltd", "Shenzhen Hui ShingTong Technology", "Yourlyon", "Fabu Technology",
                    "Shenzhen Yikesheng Technology Co Ltd", "NOR-MEM", "Cervoz Co Ltd", "Bitmain Technologies Inc.", "Facebook Inc",
                    "Shenzhen Longsys Electronics Co Ltd", "Guangzhou Siye Electronic Technology", "Silergy", "Adamway", "PZG",
                    "Shenzhen King Power Electronics", "Guangzhou ZiaoFu Tranding Co Ltd", "Shenzhen SKIHOTAR Semiconductor", "PulseRain Technology",
                    "Seeker Technology Limited", "Shenzhen OSCOO Tech Co Ltd", "Shenzhen Yze Technology Co Ltd", "Shenzhen Jieshuo Electronic Commerce",
                    "Gazda", "Hua Wei Technology Co Ltd", "Esperanto Technologies", "JinSheng Electronic (Shenzhen) Co Ltd",
                    "Shenzhen Shi Bolunshuai Technology", "Shanghai Rei Zuan Information Tech", "Fraunhofer IIS", "Kandou Bus SA", "Acer",
                    "Artmem Technology Co Ltd", "Gstar Semiconductor Co Ltd", "ShineDisk", "Shenzhen CHN Technology Co Ltd", "UnionChip Semiconductor Co Ltd",
                    "Tanbassh", "Shenzhen Tianyu Jieyun Intl Logistics", "MCLogic Inc", "Eorex Corporation", "Arm Technology (China) Co Ltd",
                    "Lexar Co Limited", "QinetiQ Group plc", "Exascend", "Hong Kong Hyunion Electronics Co Ltd", "Shenzhen Banghong Electronics Co Ltd",
                    "MBit Wireless Inc", "Hex Five Security Inc", "ShenZhen Juhor Precision Tech Co Ltd", "Shenzhen Reeinno Technology Co Ltd"
                },
                // Continuation code 0x0B
                new[] {
                    "ABIT Electronics (Shenzhen) Co Ltd", "Semidrive", "MyTek Electronics Corp", "Wxilicon Technology Co Ltd",
                    "Shenzhen Meixin Electronics Ltd", "Ghost Wolf", "LiSion Technologies Inc", "Power Active Co Ltd", "Pioneer High Fidelity Taiwan Co. Ltd",
                    "LuoSilk", "Shenzhen Chuangshifeida Technology", "Black Sesame Technologies Inc", "Jiangsu Xinsheng Intelligent Technology", "MLOONG",
                    "Quadratica LLC", "Anpec Electronics", "Xi’an Morebeck Semiconductor Tech Co", "Kingbank Technology Co Ltd", "ITRenew Inc",
                    "Shenzhen Eaget Innovation Tech Ltd", "Jazer", "Xiamen Semiconductor Investment Group", "Guangzhou Longdao Network Tech Co",
                    "Shenzhen Futian SEC Electronic Market", "Allegro Microsystems LLC", "Hunan RunCore Innovation Technology", "C-Corsa Technology",
                    "Zhuhai Chuangfeixin Technology Co Ltd", "Beijing InnoMem Technologies Co Ltd", "YooTin", "Shenzhen Pengxiong Technology Co Ltd",
                    "Dongguan Yingbang Commercial Trading Co", "Shenzhen Ronisys Electronics Co Ltd", "Hongkong Xinlan Guangke Co Ltd",
                    "Apex Microelectronics Co Ltd", "Beijing Hongda Jinming Technology Co Ltd", "Ling Rui Technology (Shenzhen) Co Ltd",
                    "Hongkong Hyunion Electronics Co Ltd", "Starsystems Inc", "Shenzhen Yingjiaxun Industrial Co Ltd",
                    "Dongguan Crown Code Electronic Commerce", "Monolithic Power Systems Inc", "WuHan SenNaiBo E-Commerce Co Ltd",
                    "Hangzhou Hikstorage Technology Co", "Shenzhen Goodix Technology Co Ltd", "Aigo Electronic Technology Co Ltd",
                    "Hefei Konsemi Storage Technology Co Ltd", "Cactus Technologies Limited", "DSIN", "Blu Wireless Technology", "Nanjing UCUN Technology Inc",
                    "Acacia Communications", "Beijinjinshengyihe Technology Co Ltd", "Zyzyx", "T-HEAD Semiconductor Co Ltd",
                    "Shenzhen Hystou Technology Co Ltd", "Syzexion", "Kembona", "Qingdao Thunderobot Technology Co Ltd", "Morse Micro",
                    "Shenzhen Envida Technology Co Ltd", "UDStore Solution Limited", "Shunlie", "Shenzhen Xin Hong Rui Tech Ltd",
                    "Shenzhen Yze Technology Co Ltd", "Shenzhen Huang Pu He Xin Technology", "Xiamen Pengpai Microelectronics Co Ltd", "JISHUN",
                    "Shenzhen WODPOSIT Technology Co", "Unistar", "UNICORE Electronic (Suzhou) Co Ltd", "Axonne Inc", "Shenzhen SOVERECA Technology Co",
                    "Dire Wolf", "Whampoa Core Technology Co Ltd", "CSI Halbleiter GmbH", "ONE Semiconductor", "SimpleMachines Inc",
                    "Shenzhen Chengyi Qingdian Electronic", "Shenzhen Xinlianxin Network Technology", "Vayyar Imaging Ltd", "Paisen Network Technology Co Ltd",
                    "Shenzhen Fengwensi Technology Co Ltd", "Caplink Technology Limited", "JJT Solution Co Ltd", "HOSIN Global Electronics Co Ltd",
                    "Shenzhen KingDisk Century Technology", "SOYO", "DIT Technology Co Ltd", "iFound", "Aril Computer Company", "ASUS",
                    "Shenzhen Ruiyingtong Technology Co", "HANA Micron", "RANSOR", "Axiado Corporation", "Tesla Corporation",
                    "Pingtouge (Shanghai) Semiconductor Co", "S3Plus Technologies SA", "Integrated Silicon Solution Israel Ltd", "GreenWaves Technologies",
                    "NUVIA Inc", "Guangzhou Shuvrwine Technology Co", "Shenzhen Hangshun Chip Technology", "Chengboliwei Electronic Business",
                    "Kowin Memory Technology Co Ltd", "Euronet Technology Inc", "SCY", "Shenzhen Xinhongyusheng Electrical", "PICOCOM",
                    "Shenzhen Toooogo Memory Technology", "VLSI Solution", "Costar Electronics Inc", "Shenzhen Huatop Technology Co Ltd",
                    "Inspur Electronic Information Industry", "Shenzhen Boyuan Computer Technology", "Beijing Welldisk Electronics Co Ltd",
                    "Suzhou EP Semicon Co Ltd", "Zhejiang Dahua Memory Technology", "Virtu Financial", "Datotek International Co Ltd",
                    "Telecom and Microelectronics Industries", "Echow Technology Ltd", "APEX-INFO", "Yingpark", "Shenzhen Bigway Tech Co Ltd"
                },
                // Continuation code 0x0C
                new[] {
                    "Beijing Haawking Technology Co Ltd", "Open HW Group", "JHICC", "ncoder AG", "ThinkTech Information Technology Co",
                    "Shenzhen Chixingzhe Technology Co Ltd", "Biao Ram Technology Co Ltd", "Shenzhen Kaizhuoyue Electronics Co Ltd",
                    "Shenzhen YC Storage Technology Co Ltd", "Shenzhen Chixingzhe Technology Co", "Wink Semiconductor (Shenzhen) Co Ltd", "AISTOR",
                    "Palma Ceia SemiDesign", "EM Microelectronic-Marin SA", "Shenzhen Monarch Memory Technology", "Reliance Memory Inc", "Jesis",
                    "Espressif Systems (Shanghai) Co Ltd", "Shenzhen Sati Smart Technology Co Ltd", "NeuMem Co Ltd", "Lifelong",
                    "Beijing Oitech Technology Co Ltd", "Groupe LDLC", "Semidynamics Technology Services SLU", "swordbill", "YIREN",
                    "Shenzhen Yinxiang Technology Co Ltd", "PoweV Electronic Technology Co Ltd", "LEORICE", "Waymo LLC", "Ventana Micro Systems",
                    "Hefei Guangxin Microelectronics Co Ltd", "Shenzhen Sooner Industrial Co Ltd", "Horizon Robotics", "Tangem AG",
                    "FuturePath Technology (Shenzhen) Co", "RC Module", "Timetec International Inc", "ICMAX Technologies Co Limited",
                    "Lynxi Technologies Ltd Co", "Guangzhou Taisupanke Computer Equipment", "Ceremorphic Inc", "Biwin Storage Technology Co Ltd",
                    "Beijing ESWIN Computing Technology", "WeForce Co Ltd", "Shenzhen Fanxiang Information Technology", "Unisoc", "YingChu", "GUANCUN",
                    "IPASON", "Ayar Labs", "Amazon", "Shenzhen Xinxinshun Technology Co", "Galois Inc", "Ubilite Inc", "Shenzhen Quanxing Technology Co Ltd",
                    "Group RZX Technology LTDA", "Yottac Technology (XI’AN) Cooperation", "Shenzhen RuiRen Technology Co Ltd", "Group Star Technology Co Ltd",
                    "RWA (Hong Kong) Ltd", "Genesys Logic Inc", "T3 Robotics Inc.", "Biostar Microtech International Corp",
                    "Shenzhen SXmicro Technology Co Ltd", "Shanghai Yili Computer Technology Co", "Zhixin Semicoducotor Co Ltd", "uFound",
                    "Aigo Data Security Technology Co. Ltd", ".GXore Technologies", "Shenzhen Pradeon Intelligent Technology", "Power LSI", "PRIME",
                    "Shenzhen Juyang Innovative Technology", "CERVO", "SiEngine Technology Co., Ltd.", "Beijing Unigroup Tsingteng MicroSystem",
                    "Brainsao GmbH", "Credo Technology Group Ltd", "Shanghai Biren Technology Co Ltd", "Nucleu Semiconductor",
                    "Shenzhen Guangshuo Electronics Co Ltd", "ZhongsihangTechnology Co Ltd", "Suzhou Mainshine Electronic Co Ltd.",
                    "Guangzhou Riss Electronic Technology", "Shenzhen Cloud Security Storage Co", "ROG", "Perceive", "e-peas", "Fraunhofer IPMS",
                    "Shenzhen Daxinlang Electronic Tech Co", "Abacus Peripherals Private Limited", "OLOy Technology", "Wuhan P&S Semiconductor Co Ltd",
                    "Sitrus Technology", "AnHui Conner Storage Co Ltd", "Rochester Electronics", "Wuxi Petabyte Technologies Co Ltd", "Star Memory",
                    "Agile Memory Technology Co Ltd", "MEJEC", "Rockchip Electronics Co Ltd", "Dongguan Guanma e-commerce Co Ltd",
                    "Rayson Hi-Tech (SZ) Limited", "MINRES Technologies GmbH", "Himax Technologies Inc", "Shenzhen Cwinner Technology Co Ltd", "Tecmiyo",
                    "Shenzhen Suhuicun Technology Co Ltd", "Vickter Electronics Co. Ltd.", "lowRISC", "EXEGate FZE", "Shenzhen 9 Chapter Technologies Co",
                    "Addlink", "Starsway", "Pensando Systems Inc.", "AirDisk", "Shenzhen Speedmobile Technology Co", "PEZY Computing",
                    "Extreme Engineering Solutions Inc", "Shangxin Technology Co Ltd", "Shanghai Zhaoxin Semiconductor Co", "Xsight Labs Ltd",
                    "Hangzhou Hikstorage Technology Co", "Dell Technologies", "Guangdong StarFive Technology Co"
                },
                // Continuation code 0x0D
                new[] {
                    "TECOTON", "Abko Co Ltd", "Shenzhen Feisrike Technology Co Ltd", "Shenzhen Sunhome Electronics Co Ltd", "Global Mixed-mode Technology Inc",
                    "Shenzhen Weien Electronics Co. Ltd.", "Shenzhen Cooyes Technology Co Ltd", "Keymos Electronics Co., Limited",
                    "E-Rockic Technology Company Limited", "Aerospace Science Memory Shenzhen", "Shenzhen Quanji Technology Co Ltd", "Dukosi",
                    "Maxell Corporation of America", "Shenshen Xinxintao Electronics Co Ltd", "Zhuhai Sanxia Semiconductor Co Ltd", "Groq Inc", "AstraTek",
                    "Shenzhen Xinyuze Technology Co Ltd", "All Bit Semiconductor", "ACFlow", "Shenzhen Sipeed Technology Co Ltd", "Linzhi Hong Kong Co Limited",
                    "Supreme Wise Limited", "Blue Cheetah Analog Design Inc", "Hefei Laiku Technology Co Ltd", "Zord", "SBO Hearing A/S",
                    "Regent Sharp International Limited", "Permanent Potential Limited", "Creative World International Limited",
                    "Base Creation International Limited", "Shenzhen Zhixin Chuanglian Technology", "Protected Logic Corporation", "Sabrent", "Union Memory",
                    "NEUCHIPS Corporation", "Ingenic Semiconductor Co Ltd", "SiPearl", "Shenzhen Actseno Information Technology",
                    "RIVAI Technologies (Shenzhen) Co Ltd", "Shenzhen Sunny Technology Co Ltd", "Cott Electronics Ltd", "Shanghai Synsense Technologies Co Ltd",
                    "Shenzhen Jintang Fuming Optoelectronics", "CloudBEAR LLC", "Emzior, LLC", "Ehiway Microelectronic Science Tech Co",
                    "UNIM Innovation Technology (Wu XI)", "GDRAMARS", "Meminsights Technology", "Zhuzhou Hongda Electronics Corp Ltd", "Luminous Computing Inc",
                    "PROXMEM", "Draper Labs", "ORICO Technologies Co. Ltd.", "Space Exploration Technologies Corp", "AONDEVICES Inc"
                }
            };

            // Lookup name by continuation code and manufacturer's ID
            byte jedecContinuationCode = (byte)(manufacturerId >> 8);
            byte jedecManufacturerCode = (byte)(manufacturerId & 0xFF);

            if (jedecContinuationCode < manufacturerNameTable.Length) {
                for (int i = 0; i < manufacturerCodeTable.Length; i++) {
                    if (jedecManufacturerCode == manufacturerCodeTable[i]) {
                        return manufacturerNameTable[jedecContinuationCode][i];
                    }
                }
            }
            
            // Unknown
            return "";
        }

        /// <summary>
        /// Gets model name from SPD contents
        /// </summary>
        /// <param name="input">SPD contents</param>
        /// <returns>Model part number</returns>
        public static string GetModulePartNumberName(byte[] input) {

            int modelNameStart;
            int modelNameEnd;

            switch (GetRamType(input)) {

                // Part number location for DDR5 SPDs
                case RamType.DDR5:
                    modelNameStart = 0x209;
                    modelNameEnd   = 0x226;
                    break;

                // Part number location for DDR4 SPDs
                case RamType.DDR4:
                    modelNameStart = 0x149;
                    modelNameEnd   = 0x15C;
                    break;

                // Part number location for DDR3 SPDs
                case RamType.DDR3:
                case RamType.DDR2_FB_DIMM:
                    modelNameStart = 0x80;
                    modelNameEnd   = 0x91;
                    break;

                // Part number for Kingston DDR2 and DDR SPDs
                case RamType.DDR2 when GetModuleManufacturer(input).StartsWith("Kingston"):
                case RamType.DDR  when GetModuleManufacturer(input).StartsWith("Kingston"):
                    modelNameStart = 0xF0;
                    modelNameEnd   = 0xFF;
                    break;

                // Part number location for DDR2 and older RAM SPDs
                default:
                    modelNameStart = 0x49;
                    modelNameEnd   = 0x5A;
                    break;
            }

            if (input.Length < modelNameEnd) {
                throw new InvalidDataException("Incomplete SPD Data");
            }

            char[] chars = new char[modelNameEnd - modelNameStart + 1];

            for (int i = 0; i < chars.Length; i++) {
                chars[i] = (char)input[modelNameStart + i];
            }

            return Data.BytesToString(chars);
        }

        /// <summary>
        /// Defines basic memory type byte value
        /// </summary>
        public enum RamType {
            UNKNOWN       = 0x00,
            SDRAM         = 0x04,
            DDR           = 0x07,
            DDR2          = 0x08,
            [Description("DDR2 FB DIMM")]
            DDR2_FB_DIMM  = 0x09,
            DDR3          = 0x0B,
            LPDDR3        = 0x0F,
            DDR4          = 0x0C,
            DDR4E         = 0x0E,
            LPDDR4        = 0x10,
            LPDDR4X       = 0x11,
            DDR5          = 0x12,
            LPDDR5        = 0x13,
            [Description("DDR5 NVDIMM-P")]
            DDR5_NVDIMM_P = 0x14,
        }

        /// <summary>
        /// Defines SPD sizes
        /// </summary>
        public enum DataLength {
            UNKNOWN       = 0,
            MINIMUM       = 256, //DDR3, DDR2, DDR, and SDRAM
            DDR4          = 512,
            DDR5          = 1024,
        }
    }
}