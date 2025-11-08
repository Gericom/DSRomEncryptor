# DSRomEncryptor
Tool to prepare DS and DSi roms for use on a cartridge. The tool inserts the necessary NTR and TWL blowfish key blocks and encrypts the NTR secure area (0x4000-0x4800).

## Usage
`DSRomEncryptor [--dsidev] input.nds output.nds`

### Arguments
- `--dsidev` - (optional) insert dev twl blowfish instead of retail, for use with twl dev units;
- `input.nds` - path to the input nds file;
- `output.nds` - path to the output nds file.

## Blowfish tables
The blowfish tables to be used by DSRomEncryptor are not included and need to be supplied by the user. The files described below go in the executable directory of DSRomEncryptor. Twl blowfish is only necessary when processing twl hybrid or exclusive roms, and the dev variant is only used when using the `--dsidev` flag.

- Ntr blowfish
    - Either `ntrBlowfish.bin` containing the ntr blowfish table (SHA1: `84E467F2485078E401A17A5F231E3FE6E9686648`)
    - Or a ds arm7 bios dump named `biosnds7.rom` (SHA1: `24F67BDEA115A2C847C8813A262502EE1607B7DF`)
- Twl blowfish (retail)
    - Either `twlBlowfish.bin` containing the twl blowfish table (SHA1: `2DEA11191F28C6CC1956DADB8941AFFD4B2B5102`)
    - Or a dsi arm7i bios dump named `biosdsi7.rom` (SHA1: `A3AA751EB6BDAAF8A827BA9E03576A6F1AB0F547` for an incomplete dump or `C7C7570BFE51C3C7C5DA3B01331B94E7E7CB4F53` for a complete dump)
- Twl blowfish (dev)
    - `twlDevBlowfish.bin` containing the dev twl blowfish table (SHA1: `CFF62F24444F5494001F019D505F9C51D40FC8B3`)

## License
DSRomEncryptor is licensed under the MIT License, see [LICENSE](./LICENSE.txt) for more information.
