/*
    Arduino based EEPROM SPD reader and writer
   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   For overclockers and PC hardware enthusiasts

   Repos:   https://github.com/1a2m3/SPD-Reader-Writer
   Support: https://forums.evga.com/FindPost/3053544
   Donate:  https://paypal.me/mik4rt3m

*/

namespace SpdReaderWriterCore {
    /// <summary>
    /// Intel: ICH/PCH device ID (LPC/eSPI controller or ISA bridge)
    /// AMD:   SMBus device ID
    /// </summary>
    public enum DeviceId : ushort {

        // Invalid
        Invalid = 0xFFFF,

        // Old pre-PCH hardware

        #region ICH
        ICH     = 0x2410,
        ICH0    = 0x2420,
        ICH2    = 0x2440,
        ICH2M   = 0x244C,
        ICH3    = 0x2480,
        ICH3M   = 0x248C,
        ICH4    = 0x24C0,
        ICH4M   = 0x24CC,
        CICH    = 0x2450,
        ICH5    = 0x24D0,
        ICH6M   = 0x2641,
        ICH6W   = 0x2642,
        ICH7DH  = 0x27B0,
        ICH7    = 0x27B8,
        ICH7M   = 0x27B9,
        ICH7MDH = 0x27BD,
        ICH8    = 0x2810,
        ICH8ME  = 0x2811,
        ICH8DH  = 0x2812,
        ICH8DO  = 0x2814,
        ICH8M   = 0x2815,
        ICH9DH  = 0x2912,
        ICH9DO  = 0x2914,
        ICH9R   = 0x2916,
        ICH9ME  = 0x2917,
        ICH9    = 0x2918,
        ICH9M   = 0x2919,
        ICH10DO = 0x3A14,
        ICH10R  = 0x3A16,
        ICH10   = 0x3A18,
        ICH10D  = 0x3A1A,
        #endregion

        // DDR3

        #region LGA1156
        H55 = 0x3B06,
        H57 = 0x3B08,
        P55 = 0x3B02,
        Q57 = 0x3B0A,
        #endregion

        #region LGA1155
        B65 = 0x1C50,
        B75 = 0x1E49,
        H61 = 0x1C5C,
        H67 = 0x1C4A,
        H77 = 0x1E4A,
        P67 = 0x1C46,
        Q65 = 0x1C4C,
        Q67 = 0x1C4E,
        Q75 = 0x1E48,
        Q77 = 0x1E47,
        Z68 = 0x1C44,
        Z75 = 0x1E46,
        Z77 = 0x1E44,
        #endregion

        #region LGA1150
        B85 = 0x8C50,
        H81 = 0x8C5C,
        H87 = 0x8C4A,
        H97 = 0x8CC6,
        Q85 = 0x8C4C,
        Q87 = 0x8C4E,
        Z87 = 0x8C44,
        Z97 = 0x8CC4,
        #endregion

        #region MOBILE 5/6/7/8/9 Series
        HM55 = 0x3B09,
        HM57 = 0x3B0B,
        HM65 = 0x1C49,
        HM67 = 0x1C4B,
        HM70 = 0x1E5E,
        HM75 = 0x1E5D,
        HM76 = 0x1E59,
        HM77 = 0x1E57,
        HM86 = 0x8C49,
        HM87 = 0x8C4B,
        HM97 = 0x8CC3,
        NM10 = 0x27BC,
        NM70 = 0x1E5F,
        PM55 = 0x3B03,
        QM57 = 0x3B07,
        QM67 = 0x1C4F,
        QM77 = 0x1E55,
        QM87 = 0x8C4F,
        QS57 = 0x3B0F,
        QS67 = 0x1C4D,
        QS77 = 0x1E56,
        UM67 = 0x1C47,
        UM77 = 0x1E58,
        #endregion

        // DDR4

        #region LGA1151
        B150  = 0xA148,
        B250  = 0xA2C8,
        B360  = 0xA308,
        B365  = 0xA2CC,
        C232  = 0xA14A,
        C236  = 0xA149,
        C242  = 0xA30A,
        C246  = 0xA309,
        CM236 = 0xA150,
        CM238 = 0xA154,
        CM246 = 0xA30E,
        H110  = 0xA143,
        H170  = 0xA144,
        H270  = 0xA2C4,
        H310  = 0xA303,
        H310D = 0x438E,
        H310M = 0xA2CA,
        H370  = 0xA304,
        HM170 = 0xA14E,
        HM175 = 0xA152,
        HM370 = 0xA30D, // aka HM470
        Q150  = 0xA147,
        Q170  = 0xA146,
        Q250  = 0xA2C7,
        Q270  = 0xA2C6,
        Q370  = 0xA306,
        QM170 = 0xA14D,
        QM175 = 0xA153,
        QM370 = 0xA30C,
        Z170  = 0xA145,
        Z270  = 0xA2C5,
        Z370  = 0xA2C9,
        Z390  = 0xA305,
        #endregion

        #region LGA1200
        B460 = 0xA3C8,
        B560 = 0x4387,
        C252 = 0x438C,
        C256 = 0x438D,
        H410 = 0xA3DA,
        H470 = 0x0684,
        H510 = 0x4388,
        H570 = 0x4386,
        Q470 = 0x0687,
        Q570 = 0x4384,
        W480 = 0x0697,
        W580 = 0x438F,
        Z490 = 0x0685,
        Z590 = 0x4385,
        #endregion

        #region LGA2066
        X299 = 0xA2D2, // CPU SMBus x2 (8086h:2085h)
        C422 = 0xA2D3, // Same as X299
        #endregion

        // DDR4 & DDR5

        #region LGA1700
        B660  = 0x7A86,
        B760  = 0x7A06,
        H610  = 0x7A87,
        H670  = 0x7A85,
        H770  = 0x7A05,
        HM670 = 0x7A8C,
        Q670  = 0x7A83,
        W680  = 0x7A88,
        WM690 = 0x7A8D,
        Z690  = 0x7A84,
        Z790  = 0x7A04,
        #endregion

        // AMD
        ZEN = 0x790B, // AM4/ZEN
        FCH = 0x780B, // FM1/FM2(+)

        // Nvidia
        nForce2        = 0x0064,
        nForce2_Ultra  = 0x0084,
        nForce3_Pro150 = 0x00D4,
        nForce3_250Gb  = 0x00E4,
        nForce4        = 0x0052,
        nForce4_MCP04  = 0x0034,
        MCP51          = 0x0264,
        MCP55          = 0x0368,
        MCP61          = 0x03EB,
        MCP65          = 0x0446,
        MCP67          = 0x0542,
        MCP73          = 0x07D8,
        MCP78S         = 0x0752,
        MCP79          = 0x0AA2,

        // VIA
        VT8231    = 0x3068,
        VT8233    = 0x3074,
        VT8233A   = 0x3147,
        VT8235    = 0x3177,
        VT8237A   = 0x3337,
        VT8237R   = 0x3227,
        VT8237S   = 0x3372,
        VT8251    = 0x3287,
        VT82C596A = 0x3050,
        VT82C596B = 0x3051,
        VT82C686x = 0x3057, // VT82C686A (rev 0x30), VT82C686B (rev 0x40)
        CX700     = 0x8324,
        VX8x0     = 0x8353, // VX800 & VX820
        VX8x5     = 0x8409, // VX855 & VX875
        VX900     = 0x8410,
    }
}