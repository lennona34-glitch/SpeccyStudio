;;-----------------------------------------------------------------------------
;
;;  Project Name:	Cybernoid
;;     File Name:	Cybernoid-tap.tap
;
;;   Description:	Cybernoid Game as per http://www.worldofspectrum.org/infoseekid.cgi?id=0001196
;
;;-----------------------------------------------------------------------------
;;                       A U T H O R   I D E N T I T Y
;;-----------------------------------------------------------------------------
;
;;	Initials	Name
;;	--------	-----------------------------------------------
;;	DB			Derek Bolli
;;  HC          Hewson Consultants Ltd, Raffaele Cecco, Nick Jones, J. Dave Rogers, John M. Phillips
;
;;-----------------------------------------------------------------------------
;;                      R E V I S I O N   H I S T O R Y
;;-----------------------------------------------------------------------------
;
;;	  Date		Time	Author	Description
;;	--------	-----	------	---------------------------------------------
;;	25/08/02	11:46	DB		Template Ported from Zeus assembler to zasm assembler
;;	18/07/02	20:10	DB		Template Original version (converted from 2DMaze.Z80)
;;  01/01/88    12:12   HC      Cybernoid Game by Raffaele Cecco, Nick Jones, J. Dave Rogers, John M. Phillips
;
;;-----------------------------------------------------------------------------
;;           C O P Y R I G H T  N O T I C E  A N D  D I S C L A I M E R
;;-----------------------------------------------------------------------------
;;  Cybernoid Game (C) 1988 Hewson Consultants Ltd
;;  Template Portions (c) Copyright 1984-2014, Derek Bolli (dbolli at bigpond.net.au)
;;  ALL RIGHTS RESERVED
;;  Permission to use, copy, modify, and distribute this software for
;;  non-commercial purposes and without fee is hereby granted, provided
;;  that the above copyright notice appear in all copies and that both the
;;  copyright notice and this permission notice appear in supporting
;;  documentation, and that the author of this software is notified of
;;  the existence of the modified software.
;
;;  THE MATERIAL EMBODIED ON THIS SOFTWARE IS PROVIDED TO YOU "AS-IS"
;;  AND WITHOUT WARRANTY OF ANY KIND, EXPRESS, IMPLIED OR OTHERWISE,
;;  INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF MERCHANTABILITY OR
;;  FITNESS FOR A PARTICULAR PURPOSE.  IN NO EVENT SHALL THE
;;  AUTHOR BE LIABLE TO YOU OR ANYONE ELSE FOR ANY DIRECT,
;;  SPECIAL, INCIDENTAL, INDIRECT OR CONSEQUENTIAL DAMAGES OF ANY
;;  KIND, OR ANY DAMAGES WHATSOEVER, INCLUDING WITHOUT LIMITATION,
;;  LOSS OF PROFIT, LOSS OF USE, SAVINGS OR REVENUE, OR THE CLAIMS OF
;;  THIRD PARTIES, WHETHER OR NOT THE AUTHOR HAS BEEN
;;  ADVISED OF THE POSSIBILITY OF SUCH LOSS, HOWEVER CAUSED AND ON
;;  ANY THEORY OF LIABILITY, ARISING OUT OF OR IN CONNECTION WITH THE
;;  POSSESSION, USE OR PERFORMANCE OF THIS SOFTWARE.
;;-----------------------------------------------------------------------------  


; *************
; * Cybernoid *
; *************

; Cybernoid Game (C) 1988 Hewson Consultants Ltd
; Template Portions (C) Derek Bolli 1984-2014

; ***************
; Version History:

;   DB    1988      0.0d  Initial version


#target				tap				; declare target file format

; . . . . . . . . . . .
; Equates

ERROR1		equ		$0008
PRINTA1		equ		$0010
GETCHAR		equ		$0018
NEXTCHAR	equ		$0020
MASKINT		equ		$0038

KEYSCAN		EQU		$028E			
BEEPER		equ		$03B5			
BEEP		equ		$03F8
LDBYTES		equ		$0556			
POSTORE		equ		$0ADC
POFETCH		equ		$0B03
POATTR		equ		$0BDB
TEMPS		equ		$0D4D
ROMCLS		equ		$0D6B
CLSET		equ		$0DD9
CLADDR		equ		$0E9B
EDLOOP		equ		$0F38
EDEDIT		equ		$0FA9
EDERROR		equ		$107F
CHANOPEN	equ		$1601			
STRDATA1	equ		$1727			
NEXT2NUM	equ		$1C79			
EXPT2NUM	equ		$1C7A
EXPT1NUM	equ		$1C82
EXPTEXP		equ		$1C8C			
FNDINT1		equ		$1E94			
FINDINT2	equ		$1E99
PIXADD		equ		$22AA
;PIXADD		equ		OURPIXADD		; Point to our modified PIXADD
POINT		equ		$22CB			; Coords on stack on entry (use STACKBC), binary result on stack at exit (use FPTOA)
CRGRE1		equ		$233B			; On entry stack = X Y Z -> Draws circle at X,Y with radius Z
DRAWLINE	equ		$24B7
;DRAWLN2	equ		$24BA			; Use our routine below...
SCANNING	equ		$24FB
STKSTORE	equ		$2AB6
STKFETCH	equ		$2BF1
DECTOFP		equ		$2C9B
STACKA		equ		$2D28
STACKBC		equ		$2D2B
FPTOBC		equ		$2DA2
FPTOA		equ		$2DD5
PRINTFP		equ		$2DE3
STACKNUM	equ		$33B4	

DFILE		equ		$4000			; Screen coords 0 - 255, 0 (top) - 175 (or 191)
ATTRS		equ		$5800			; Screen chars 0 - 31, 0 - 21 (or 23)

COMMA_CONTROL equ		$06				
CHAR_ENTER	equ		$0D				; ASCII CR
INK_CONTROL	equ		$10				
PAPER_CONTROL	equ		$11				
INVERSE_CONTROL	equ		$14				
AT_CONTROL	equ		$16				

CHAR_SPACE	equ		$20				; ASCII Space

GRAPHICS_SHIFT_3	equ		$8C			; Graphic Shift 3

GRAPHICS_A	equ		$90				; Graphic Char A

KWRDSCREENSTR	equ		$AA				; BASIC Keywords (see pp358-360 of Mastering Machine Code by Toni Baker)
KWRDAT			equ		$AC
KWRDCODE		equ		$AF
KWRDVAL			equ		$B0
KWRDEXP			equ		$B9
KWRDUSR			equ		$C0
KWRDLINE		equ		$CA
KWRDCAT			equ		$CF
KWRDBEEP		equ		$D7
KWRDINK			equ		$D9
KWRDPAPER		equ		$DA
KWRDBRIGHT		equ		$DC
KWRDLPRINT		equ		$E0
KWRDSTOP		equ		$E2
KWRDREAD		equ		$E3
KWRDDATA		equ		$E4
KWRDRESTORE		equ		$E5
KWRDBORDER		equ		$E7
KWRDREM			equ		$EA
KWRDLOAD		equ		$EF
KWRDPAUSE		equ		$F2
KWRDPOKE		equ		$F4
KWRDPRINT		equ		$F5
KWRDRANDOMIZE	equ		$F9
KWRDCLS			equ		$FB
KWRDCLEAR		equ		$FD

; . . . . . . . . . . .
; Interface 1 Edition 2 equates (as per Fuse 1.0)

MAINROM		equ		$00
CALBAS		equ		$10				; Call a main ROM routine
SHERR		equ		$20
ROMERR		equ		$28
NEWVAR		equ		$30
ERR6			equ		$01F0
NREPORTC		equ		$052F
STEND		equ		$05B7
END1			equ		$05C1			
EXPTNUM		equ		$061E
NREPORT2		equ		$0663
CHECKM2		equ		$066D
NREPORT3		equ		$062D
UNPAGE		equ		$0700
NREPORTN		equ		$0906
OPRSCH		equ		$0B17
OPSTREAM		equ		$0B51
TCHANOUT		equ		$0C3A
BCHANOUT		equ		$0D07
SETTMCH		equ		$10A5
DELMBUF		equ		$119F
RWFERR		equ		$1132
GETRLP		equ		$125F
RSSH2		equ		$11A3
GHDRC		equ		$11A5
GETMHD2		equ		$13A9
RESBMAP		equ		$13E3
DECSECT		equ		$13F7
CHKSHDR		equ		$1426
CLOSE		equ		$1718
SELDRIVE		equ		$1532
OUTMBUF		equ		$15B3
GETMBLK		equ		$15F2
DELAYBC		equ		$1652
RDSECTOR		equ		$1F3F
RSSH			equ		$1AC5
FREESECT		equ		$1D43
PRCHAR		equ		$1D71
INCHK		equ		$1E49
DISPHEX		equ		$14D6
DISPHEX2		equ		$14ED
DISPCH		equ		$14F8

; . . . . . . . . . . .
; Constants

FALSE		equ		$00
TRUE		equ		$01


KSTATE		equ 	$5C00 			; Keyboard bytes
LASTK		equ 	$5C08			; Last keypress
REPDEL		equ		$5C09			; Delay until repeat starts (Default 35)
REPPER		equ 	$5C0A			; Key repeat delay (Default 5)
DEFADD		equ		$5C0B			; Pointer to DEF FN params during eval
KDATA		equ		$5C0D			; Second byte of colour control from keyboard
TVDATA		equ		$5C0E			; Both bytes of colour, AT, TAB controls
STRMS		equ		$5C10			; Address of Stream data
CHARS		equ		$5C36			; Address - $0100 of character definitions in ROM ($3D00)
RASP		equ		$5C38			; Duration of buzz (i.e. if no mem for input)
PIP			equ		$5C39			; Duration of keyboard click (Default 0)
ERRNR		equ		$5C3A			; Error Report Number - 1 <<< IY Points here
FLAGS		equ		$5C3B			; Flags (bit 5 = LASTK is valid, bit 1 = Printer output)
TVFLAG		equ		$5C3C			; Flag bits
ERRSP		equ		$5C3D			; Pointer to top of stack for RET on error
LISTSP		equ		$5C3F			; Pointer to return from auto list
MODE		equ		$5C41			; Keyword, Extended, Graphics or Letters/Caps
NEWPPC		equ		$5C42			; Next line number of program
NSPPC		equ		$5C44			; Next statement offset (in next line)
PPC			equ		$5C45			; Current line number of program
SUBPPC		equ		$5C47			; Current statement offset (in current line)
BORDCR		equ		$5C48			; Bits 5, 4, 3 = border colour
EPPC		equ		$5C49			; Program edit cursor line number
VARS		equ		$5C4B			; Pointer to first variable
DEST		equ		$5C4D			; Pointer to variable during assignment
CHANS		equ		$5C4F			; Pointer to first channel area
CURCHL		equ		$5C51			; Pointer to current channel area
PROG		equ		$5C53			; Pointer to first line in program
NXTLIN		equ		$5C55			; Pointer to next program line
DATADD		equ		$5C57			; Pointer to just past last data item read
ELINE		equ		$5C59			; Pointer to edit line
KCUR		equ		$5C5B			; Pointer to cursor
CHADD		equ		$5C5D			; Pointer to next char
XPTR		equ		$5C5F			; Pointer to syntax error
WORKSP		equ		$5C61			; Pointer to workspace
STKBOT		equ		$5C63			; Pointer to calculator stack base
STKEND		equ		$5C65			; Pointer to topmost memory address used by BASIC
BREG		equ		$5C67			; Byte used for B register of calculator
MEM			equ		$5C68			; Pointer to calculator memory
FLAGS2		equ		$5C6A			; Bit flags
DFSZ		equ		$5C6B			; Lines in lower screen (Default 2)
STOP		equ		$5C6C			; Line number of top line of auto list
OLDPPC		equ		$5C6E			; Continuation line number
OSPCC		equ		$5C70			; Continuation statement number
FLAGX		equ		$5C71			; Bit flags
STRLEN		equ		$5C72			; String length during assignment
TADDR		equ		$5C74			; Pointer to next syntax table item
SEED		equ		$5C76			; Word used by random number routines
FRAMES		equ		$5C78			; Frame counter low, med, high bytes
UDG			equ		$5C7B			; Pointer to bit patterns for user defined graphics
COORDS		equ		$5C7D			; Plot x,y values
PPOSN		equ		$5C7F			; Printer position (Range 33-02)
PRCC		equ		$5C80			; Low byte of next address in printer buffer
									; $5C81 Unused
ECHOE		equ		$5C82			; Column, line for input buffer
DFCC		equ		$5C84			; Next screen location for print
DFCCL		equ		$5C86			; Next screen location for print (lower screen)
SPOSN		equ		$5C88			; Screen column (32-02), screen row (24-01)
SPOSNL		equ		$5C8A			; Screen column (32-02), screen row (24-01) (lower screen)
SCRCT		equ		$5C8C			; Remaining scroll count + 1 (0 is maximum)
ATTRP		equ		$5C8D			; Screen colours
MASKP		equ 	$5C8E			; Defines the transparent screen colours
ATTRT		equ		$5C8F			; Temporary colours
MASKT		equ		$5C90			; Defines the transparent temporary screen colours
PFLAG		equ		$5C91			; Bit flags
MEMBOT		equ		$5C92			; Scratchpad area for calculator routines
SPARE2		equ		$5CB0			; Pointer used by NMI routine at $0066
RAMTOP		equ		$5CB2			; Pointer to highest stack byte (value $3E)
PRAMT		equ 	$5CB4			; Highest byte that passed RAM test


KSTATEOFF	equ		KSTATE-ERRNR	; Offsets for use in (IY+n)
LASTKOFF	equ		LASTK-ERRNR		
REPDELOFF	equ		REPDEL-ERRNR	
REPPEROFF	equ		REPPER-ERRNR	
DEFADDOFF	equ		DEFADD-ERRNR	
KDATAOFF	equ		KDATA-ERRNR		
TVDATAOFF	equ		TVDATA-ERRNR	
STRMSOFF	equ		STRMS-ERRNR		
CHARSOFF	equ		CHARS-ERRNR		
RASPOFF		equ		RASP-ERRNR		
PIPOFF		equ		PIP-ERRNR		
ERRNROFF	equ		ERRNR-ERRNR		
FLAGSOFF	equ		FLAGS-ERRNR		
TVFLAGOFF	equ		TVFLAG-ERRNR	
ERRSPOFF	equ		ERRSP-ERRNR		
LISTSPOFF	equ		LISTSP-ERRNR	
MODEOFF		equ		MODE-ERRNR		
NEWPPCOFF	equ		NEWPPC-ERRNR	
NSPPCOFF	equ		NSPPC-ERRNR		
PPCOFF		equ		PPC-ERRNR		
SUBPPCOFF	equ		SUBPPC-ERRNR	
BORDCROFF	equ		BORDCR-ERRNR	
EPPCOFF		equ		EPPC-ERRNR		
VARSOFF		equ		VARS-ERRNR		
DESTOFF		equ		DEST-ERRNR		
CHANSOFF	equ		CHANS-ERRNR		
CURCHLOFF	equ		CURCHL-ERRNR	
PROGOFF		equ		PROG-ERRNR		
NXTLINOFF	equ		NXTLIN-ERRNR	
DATADDOFF	equ		DATADD-ERRNR	
ELINEOFF	equ		ELINE-ERRNR		
KCUROFF		equ		KCUR-ERRNR		
CHADDOFF	equ		CHADD-ERRNR		
XPTROFF		equ		XPTR-ERRNR		
WORKSPOFF	equ		WORKSP-ERRNR	
STKBOTOFF	equ		STKBOT-ERRNR	
STKENDOFF	equ		STKEND-ERRNR	
BREGOFF		equ		BREG-ERRNR		
MEMOFF		equ		MEM-ERRNR		
FLAGS2OFF	equ		FLAGS2-ERRNR	
DFSZOFF		equ		DFSZ-ERRNR		

ATTRTOFF		equ		ATTRT-ERRNR	

; -----------------------------
; THE 'SHADOW' SYSTEM VARIABLES
; -----------------------------

FLAGS3		equ $5CB6				; $5CB6 FLAGS3 IY+$7C - Flags
VECTOR		equ $5CB7				; $5CB7 VECTOR Address used to extend BASIC.
SBRT			equ $5CB9				; $5CB9 SBRT 10 bytes of Z80 code to Page ROM.
BAUD			equ $5CC3				; $5CC3 BAUD =(3500000/(26*baud rate)) -2
NTSTAT		equ $5CC5				; $5CC5 NTSTAT Own network station number.
IOBORD		equ $5CC6				; $5CC6 IOBORD Border colour during I/O
SERFL		equ $5CC7				; $5CC7 SER_FL 2 byte workspace used by RS232
SECTOR		equ $5CC9				; $5CC9 SECTOR 2 byte workspace used by Microdrive.
CHADD_		equ $5CCB				; $5CCB CHADD_ Temporary store for CH_ADD
NTRESP		equ $5CCC				; $5CCC NTRESP Store for network response code.

NTDEST		equ $5CCD				; $5CCD NTDEST Destination station number 0 - 64.
NTSRCE		equ $5CCE				; $5CCE NTSRCE Source station number.
NTNUMB		equ $5CD0				; $5CD0 NTNUMB Network block number 0 - 65535
NTTYPE		equ $5CD2				; $5CD2 NTTYPE Header type block.
NTLEN		equ $5CD3				; $5CD3 NTLEN Data block length 0 - 255.
NTDCS		equ $5CD4				; $5CD4 NTDCS Data block checksum.
NTHDS		equ $5CD5				; $5CD5 NTHDS Header block checksum.

DSTR1		equ $5CD6				; $5CD6 D_STR1 2 byte drive number 1 - 8.
SSTR1		equ $5CD8				; $5CD8 S_STR1 Stream number 1 - 15.
LSTR1		equ $5CD9				; $5CD9 L_STR1 Device type "M", "N", "T" or "B"
NSTR1		equ $5CDA				; $5CDA N-STR1 Length of filename.
								; $5CDC (dynamic) Address of filename.

DSTR2		equ $5CDE				; $5CDE D_STR2 2 byte drive  File type.
								; $5CDF        number.       Length of
SSTR2		equ $5CE0				; $5CE0 S_STR2 Stream number.Data.
LSTR2		equ $5CE1				; $5CE1 L_STR2 Device type.  Start of
NSTR2		equ $5CE2				; $5CE2 N-STR2 Length of     data.   \
								; $5CE3        filename.     Program  \
								; $5CE4 (dynamic) Address of length. Start of
								; $5CE5 (dynamic) filename           data.

HD00			equ $5CE6				; $5CE6 HD_00 File type .      _
HD0B			equ $5CE7				; $5CE7 HD_0B Length of data.   /\
HD0D			equ $5CE9				; $5CE9 HD_0D Start of data.   /
HD0F			equ $5CEB				; $5CEB HD_0F Program length. /
HD11			equ $5CED				; $5CED HD_11 Line number.

COPIES		equ $5CEF				; $5CEF COPIES Number of copies made by SAVE.

CHREC		equ	$0D
CHDRIV		equ	$19				; Offset to DSTR1
HDNUMB		equ	$29



; . . . . . . . . . . .
; Constants

HEADERFLAG	equ $00				; As per http://k1.dyndns.org/Develop/projects/zasm/doc/z13.htm#L
DATAFLAG	equ $FF


; . . . . . . . . . . .
; BASIC Program Header


#code				0,17,HEADERFLAG	; declare code segment

			defb 0				; Program
			defm "cybernoid "	; Pad to 10 chars
			defw PROGRAM1END
			defw 10				; BASIC Line number for autostart
			defw PROGRAM1END    ; length of BASIC program without variables


; . . . . . . . . . . .
; BASIC Program


#code				0,$200,DATAFLAG	; declare code segment

; 10 BORDER 0: PAPER 0: INK 7: CLEAR 24835				; $6103
			defb 0,10   		; line number
			defb L10END-($+1)	; line length
			defb 0				; statement number
			defb KWRDBORDER		; token BORDER
        	defm "0",$0e0000000000   ; number 0, ascii & internal format
			defb ':'
			defb KWRDPAPER		; token PAPER
        	defm "0",$0e0000000000   ; number 0, ascii & internal format
			defb ':'
			defb KWRDINK		; token INK
        	defm "7",$0e0000070000   ; number 7, ascii & internal format
 			defb ':'
        	defb KWRDCLEAR		; token CLEAR
        	defm "24835",$0e0000036100   ; number 24835, ascii & internal format
L10END		defb $0D			; line end marker

; 20 LOAD "" CODE SCREEN$: PAUSE 0			; $4000
			defb 0,20			; line number
			defb L20END-($+1)	; line length
			defb 0				; statement number
			defb KWRDLOAD,'"','"',KWRDSCREENSTR     ; token LOAD, 2 quotes, token SCREEN$
 			defb ':'
        	defb KWRDPAUSE		; token PAUSE
        	defm "0",$0e0000000000   ; number 0, ascii & internal format
L20END		defb $0D			; line end marker

; 30 LOAD "" CODE 24835			; $6103
			defb 0,30			; line number
			defb L30END-($+1)	; line length
			defb 0				; statement number
			defb KWRDLOAD,'"','"',KWRDCODE     ; token LOAD, 2 quotes, token CODE
        	defm "24835",$0e0000036100   ; number 24835, ascii & internal format
L30END		defb $0D			; line end marker

; 40 RANDOMIZE USR 25860		; $6504
			defb 0,40			; line number
			defb L40END-($+1)	; line length
			defb 0				; statement number
			defb KWRDRANDOMIZE,KWRDUSR	; token RANDOMIZE, token USR
        	defm "25860",$0e0000046500	; number 25860, ascii & internal format
L40END		defb $0D                    ; line end marker

PROGRAM1END equ $


; . . . . . . . . . . .
; Code Block 1 Header


#code				0,17,HEADERFLAG	; declare code segment

CODE1START	equ $4000			; 16384

			defb 3				; Code
			defm "cybernoid "	; Pad to 10 chars
			defw CODE1END-CODE1START	; Length
			defw CODE1START		; Code Start Address
			defw 0				; Unused


; . . . . . . . . . . .
; Code Block 1 Data


#code				CODE1START,$1B00,DATAFLAG	; declare code segment

			defb $00,$03,$FF,$E0,$00,$00,$00,$00				; $4000 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4008 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4010 ........
			defb $00,$06,$00,$00,$00,$00,$00,$00				; $4018 ........
			defb $00,$00,$03,$FE,$FF,$F0,$00,$00				; $4020 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4028 ........
			defb $00,$00,$00,$00,$00,$7F,$C0,$00				; $4030 ........
			defb $00,$27,$80,$00,$00,$00,$00,$00				; $4038 .'......
			defb $07,$FF,$FC,$0F,$F1,$FF,$FF,$F8				; $4040 ........
			defb $7F,$FF,$0F,$FF,$FF,$1F,$FF,$FE				; $4048 ........
			defb $3F,$FF,$FC,$7F,$8E,$7F,$00,$07				; $4050 ?.......
			defb $00,$2A,$A0,$00,$00,$00,$00,$00				; $4058 .*......
			defb $00,$06,$00,$01,$F7,$8F,$00,$0F				; $4060 ........
			defb $07,$8F,$C1,$F3,$1E,$61,$E0,$07				; $4068 .....a..
			defb $C0,$F0,$0F,$81,$E0,$3C,$00,$00				; $4070 .....<..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4078 ........
			defb $00,$00,$07,$EF,$F7,$F8,$7F,$FF				; $4080 ........
			defb $FF,$F0,$01,$FF,$E1,$FE,$01,$FF				; $4088 ........
			defb $FC,$7F,$8F,$FF,$FF,$FF,$00,$00				; $4090 ........
			defb $00,$AA,$A0,$00,$00,$00,$00,$00				; $4098 ........
			defb $00,$00,$00,$07,$F0,$31,$FB,$F0				; $40A0 .....1..
			defb $3F,$6D,$9F,$8C,$6D,$DB,$01,$AD				; $40A8 ?m..m...
			defb $FB,$07,$F6,$DD,$F8,$00,$00,$00				; $40B0 ........
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $40B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $40C0 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $40C8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $40D0 ........
			defb $00,$0A,$A0,$00,$00,$00,$00,$00				; $40D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $40E0 ........
			defb $00,$00,$34,$64,$08,$AA,$7E,$2A				; $40E8 ..4d..~*
			defb $A9,$FE,$C0,$00,$00,$00,$00,$00				; $40F0 ........
			defb $0A,$AB,$E8,$00,$00,$00,$08,$00				; $40F8 ........
			defb $00,$01,$FF,$C0,$00,$00,$00,$00				; $4100 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4108 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4110 ........
			defb $00,$09,$80,$00,$00,$00,$00,$00				; $4118 ........
			defb $00,$00,$01,$FF,$FF,$E0,$00,$00				; $4120 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4128 ........
			defb $00,$00,$00,$00,$00,$7F,$80,$00				; $4130 ........
			defb $00,$10,$50,$00,$00,$00,$00,$00				; $4138 ..P.....
			defb $03,$FC,$00,$0F,$F3,$FC,$0F,$F0				; $4140 ........
			defb $FF,$00,$1F,$C0,$FE,$3F,$C3,$FC				; $4148 .....?..
			defb $7F,$87,$F8,$FF,$1F,$7F,$00,$06				; $4150 ........
			defb $80,$15,$50,$00,$00,$00,$00,$00				; $4158 ..P.....
			defb $00,$03,$FC,$0F,$F7,$FF,$F0,$FF				; $4160 ........
			defb $FF,$1F,$FF,$80,$3F,$C3,$FC,$7F				; $4168 ....?...
			defb $87,$F8,$FF,$1F,$E0,$FF,$00,$07				; $4170 ........
			defb $00,$E7,$00,$30,$00,$60,$00,$00				; $4178 ...0.`..
			defb $00,$00,$03,$EF,$F7,$F0,$FF,$FF				; $4180 ........
			defb $FF,$E0,$00,$FF,$C3,$FC,$00,$FF				; $4188 ........
			defb $F8,$FF,$1F,$FF,$FF,$FF,$00,$00				; $4190 ........
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $4198 .UP.....
			defb $00,$00,$00,$03,$F0,$31,$9B,$00				; $41A0 .....1..
			defb $30,$6C,$D9,$8C,$6C,$D9,$81,$8D				; $41A8 0l..l...
			defb $FB,$06,$36,$CD,$80,$00,$00,$00				; $41B0 ..6.....
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $41B8 .UP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $41C0 ........
			defb $00,$00,$00,$4C,$84,$C4,$54,$03				; $41C8 ...L..T.
			defb $E0,$00,$00,$00,$00,$00,$00,$00				; $41D0 ........
			defb $00,$51,$50,$00,$00,$00,$00,$00				; $41D8 .QP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $41E0 ........
			defb $00,$00,$10,$9B,$61,$55,$7F,$C5				; $41E8 ....aU..
			defb $54,$FE,$E0,$00,$00,$00,$00,$00				; $41F0 T.......
			defb $15,$53,$A8,$00,$00,$00,$22,$00				; $41F8 .S....".
			defb $00,$00,$FF,$80,$3F,$FF,$C0,$00				; $4200 ....?...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4208 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4210 ........
			defb $00,$0B,$80,$00,$00,$00,$00,$00				; $4218 ........
			defb $00,$00,$00,$FF,$FF,$C0,$00,$00				; $4220 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4228 ........
			defb $00,$00,$00,$00,$00,$7F,$00,$00				; $4230 ........
			defb $00,$2A,$A0,$00,$00,$00,$00,$40				; $4238 .*.....@
			defb $01,$FE,$00,$0F,$F7,$F8,$1F,$E1				; $4240 ........
			defb $FE,$00,$3F,$81,$FC,$7F,$87,$F8				; $4248 ..?.....
			defb $FF,$0F,$F1,$FE,$3F,$FF,$00,$07				; $4250 ....?...
			defb $34,$2A,$A0,$00,$00,$00,$00,$00				; $4258 4*......
			defb $00,$01,$FE,$0F,$F0,$1F,$E1,$FE				; $4260 ........
			defb $00,$3F,$FF,$C0,$7F,$87,$F8,$FF				; $4268 .?......
			defb $0F,$F1,$FE,$3F,$C0,$7F,$00,$06				; $4270 ...?....
			defb $81,$8C,$00,$30,$00,$D0,$00,$00				; $4278 ...0....
			defb $00,$00,$01,$EF,$F7,$E1,$FF,$FF				; $4280 ........
			defb $FF,$C0,$00,$7F,$83,$F8,$00,$7F				; $4288 ........
			defb $F1,$FE,$3F,$FF,$FF,$FF,$00,$00				; $4290 ..?.....
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $4298 ........
			defb $00,$00,$00,$01,$F0,$31,$9B,$F0				; $42A0 .....1..
			defb $30,$6F,$D9,$8C,$6C,$DF,$81,$8D				; $42A8 0o..l...
			defb $9B,$F6,$36,$CD,$F8,$00,$00,$00				; $42B0 ..6.....
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $42B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $42C0 ........
			defb $00,$00,$00,$90,$1B,$2A,$AA,$8F				; $42C8 .....*..
			defb $F8,$00,$00,$00,$00,$00,$00,$00				; $42D0 ........
			defb $00,$AA,$A0,$00,$00,$00,$00,$00				; $42D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $42E0 ........
			defb $00,$00,$41,$64,$02,$AA,$7F,$F8				; $42E8 ..Ad....
			defb $AA,$7C,$F0,$00,$00,$00,$00,$00				; $42F0 .|......
			defb $0A,$AA,$A8,$00,$00,$00,$08,$00				; $42F8 ........
			defb $00,$00,$7F,$C0,$1F,$FF,$80,$00				; $4300 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4308 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4310 ........
			defb $00,$0F,$00,$00,$00,$00,$00,$00				; $4318 ........
			defb $FF,$FF,$FF,$7F,$FF,$8F,$FF,$80				; $4320 ........
			defb $03,$FF,$F8,$7F,$F0,$00,$FF,$E0				; $4328 ........
			defb $01,$FF,$C0,$3F,$FC,$7F,$00,$00				; $4330 ...?....
			defb $00,$15,$50,$00,$00,$00,$00,$A0				; $4338 ..P.....
			defb $00,$FF,$00,$0F,$F7,$F0,$3F,$C3				; $4340 ......?.
			defb $FC,$00,$7F,$03,$F8,$FF,$0F,$F1				; $4348 ........
			defb $FE,$1F,$E3,$FC,$7F,$FF,$00,$06				; $4350 ........
			defb $B4,$15,$50,$00,$00,$00,$00,$00				; $4358 ..P.....
			defb $00,$00,$FF,$EF,$F0,$3F,$C3,$FC				; $4360 .....?..
			defb $00,$7F,$BF,$E0,$FF,$0F,$F1,$FE				; $4368 ........
			defb $1F,$E3,$FC,$7F,$80,$7F,$00,$07				; $4370 ........
			defb $1D,$CE,$73,$31,$80,$C3,$1C,$E6				; $4378 ..s1....
			defb $00,$00,$00,$0F,$F0,$00,$00,$00				; $4380 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4388 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4390 ........
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $4398 .UP.....
			defb $00,$00,$00,$00,$F0,$31,$9B,$F0				; $43A0 .....1..
			defb $30,$6F,$D9,$8C,$6C,$DF,$81,$8D				; $43A8 0o..l...
			defb $9B,$F6,$36,$CD,$F8,$00,$00,$00				; $43B0 ..6.....
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $43B8 .UP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $43C0 ........
			defb $00,$00,$01,$08,$64,$D4,$55,$53				; $43C8 ....d.US
			defb $FC,$00,$00,$00,$00,$00,$00,$00				; $43D0 ........
			defb $00,$51,$50,$00,$00,$00,$00,$00				; $43D8 .QP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $43E0 ........
			defb $00,$00,$20,$9B,$05,$55,$7F,$FE				; $43E8 .. ..U..
			defb $55,$33,$F0,$00,$00,$00,$00,$00				; $43F0 U3......
			defb $15,$52,$A8,$00,$00,$00,$00,$00				; $43F8 .R......
			defb $00,$00,$3F,$E0,$0F,$FF,$00,$00				; $4400 ..?.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4408 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4410 ........
			defb $00,$15,$40,$00,$00,$00,$00,$00				; $4418 ..@.....
			defb $7F,$FF,$FF,$BF,$FF,$1F,$FF,$C0				; $4420 ........
			defb $07,$FF,$F0,$FF,$F8,$01,$FF,$F0				; $4428 ........
			defb $03,$FF,$E0,$1F,$F8,$7F,$00,$00				; $4430 ........
			defb $00,$2A,$A0,$00,$00,$00,$00,$40				; $4438 .*.....@
			defb $00,$7F,$80,$0F,$F7,$FF,$FF,$87				; $4440 ........
			defb $FF,$F8,$FF,$FF,$F1,$FE,$1F,$E3				; $4448 ........
			defb $FC,$3F,$C7,$F8,$FF,$FF,$00,$06				; $4450 .?......
			defb $B4,$2A,$A0,$00,$00,$00,$00,$00				; $4458 .*......
			defb $00,$00,$7F,$EF,$F0,$7F,$87,$F8				; $4460 ........
			defb $00,$FF,$1F,$F1,$FE,$1F,$E3,$FC				; $4468 ........
			defb $3F,$C7,$F8,$FF,$00,$7F,$00,$07				; $4470 ?.......
			defb $35,$8C,$D7,$B3,$C0,$C7,$B1,$8D				; $4478 5.......
			defb $00,$00,$00,$7F,$F0,$00,$00,$00				; $4480 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4488 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4490 ........
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $4498 ........
			defb $00,$00,$00,$00,$70,$00,$00,$00				; $44A0 ....p...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $44A8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $44B0 ........
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $44B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $44C0 ........
			defb $00,$00,$06,$A0,$99,$2A,$AA,$A8				; $44C8 .....*..
			defb $7C,$00,$00,$00,$00,$10,$00,$00				; $44D0 |.......
			defb $00,$AA,$A8,$00,$00,$00,$08,$00				; $44D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $44E0 ........
			defb $00,$00,$03,$60,$2A,$AA,$7F,$FF				; $44E8 ...`*...
			defb $AA,$8F,$F8,$00,$00,$00,$00,$00				; $44F0 ........
			defb $2A,$AB,$A8,$00,$00,$00,$00,$00				; $44F8 *.......
			defb $00,$00,$1F,$F0,$1F,$FE,$00,$00				; $4500 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4508 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4510 ........
			defb $00,$15,$40,$00,$00,$00,$00,$00				; $4518 ..@.....
			defb $3F,$FF,$FF,$9F,$FE,$3F,$FF,$E0				; $4520 ?....?..
			defb $0F,$FF,$E1,$FF,$FC,$03,$FF,$F8				; $4528 ........
			defb $07,$FF,$F0,$0F,$F0,$7F,$00,$00				; $4530 ........
			defb $00,$15,$50,$00,$00,$00,$00,$00				; $4538 ..P.....
			defb $00,$3F,$C0,$0F,$F7,$FF,$FF,$0F				; $4540 .?......
			defb $FF,$F1,$FF,$FF,$E3,$FC,$3F,$C7				; $4548 ......?.
			defb $F8,$7F,$8F,$F1,$FF,$FF,$00,$07				; $4550 ........
			defb $1C,$15,$50,$00,$00,$00,$00,$00				; $4558 ..P.....
			defb $00,$00,$3F,$EF,$F7,$FF,$0F,$FF				; $4560 ..?.....
			defb $FF,$FE,$0F,$FF,$FC,$3F,$C7,$FF				; $4568 .....?..
			defb $FF,$8F,$F1,$FF,$FF,$FF,$00,$06				; $4570 ........
			defb $B5,$8C,$D6,$33,$80,$D6,$31,$8D				; $4578 ...3..1.
			defb $00,$00,$00,$3F,$F0,$FD,$9B,$F0				; $4580 ...?....
			defb $3F,$6F,$D9,$BF,$6C,$DF,$81,$8D				; $4588 ?o..l...
			defb $FB,$F6,$36,$CD,$F8,$00,$00,$00				; $4590 ..6.....
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $4598 .UP.....
			defb $00,$00,$00,$00,$30,$00,$00,$00				; $45A0 ....0...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $45A8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $45B0 ........
			defb $00,$51,$50,$00,$00,$00,$00,$00				; $45B8 .QP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $45C0 ........
			defb $00,$00,$09,$43,$64,$95,$15,$55				; $45C8 ...Cd..U
			defb $1C,$00,$00,$00,$00,$28,$00,$00				; $45D0 .....(..
			defb $01,$51,$50,$00,$00,$00,$22,$00				; $45D8 .QP...".
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $45E0 ........
			defb $00,$00,$64,$88,$55,$55,$1F,$FF				; $45E8 ..d.UU..
			defb $C5,$57,$FC,$00,$00,$00,$00,$00				; $45F0 .W......
			defb $15,$50,$E8,$00,$00,$00,$00,$00				; $45F8 .P......
			defb $00,$00,$0F,$F8,$3F,$FC,$00,$00				; $4600 ....?...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4608 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4610 ........
			defb $00,$0C,$A0,$00,$00,$00,$00,$00				; $4618 ........
			defb $1F,$FF,$FF,$0F,$FC,$7F,$FF,$F0				; $4620 ........
			defb $1F,$FF,$C3,$FF,$FE,$07,$FF,$FC				; $4628 ........
			defb $0F,$FF,$F8,$1F,$E0,$7F,$00,$00				; $4630 ........
			defb $00,$2A,$A0,$00,$00,$00,$00,$00				; $4638 .*......
			defb $00,$1F,$E0,$0F,$F7,$FF,$FE,$1F				; $4640 ........
			defb $FF,$E3,$FF,$FF,$C7,$F8,$7F,$8F				; $4648 ........
			defb $F0,$FF,$1F,$E3,$FF,$FF,$00,$00				; $4650 ........
			defb $04,$2A,$A0,$00,$00,$00,$00,$00				; $4658 .*......
			defb $00,$00,$1F,$EF,$F7,$FE,$1F,$FF				; $4660 ........
			defb $FF,$FC,$07,$FF,$F8,$7F,$87,$FF				; $4668 ........
			defb $FF,$1F,$E3,$FF,$FF,$FF,$00,$06				; $4670 ........
			defb $9D,$8C,$73,$B9,$C0,$63,$9C,$E6				; $4678 ..s..c..
			defb $00,$00,$00,$1F,$F0,$FD,$9B,$F0				; $4680 ........
			defb $3F,$6F,$D9,$BF,$6E,$DF,$81,$DD				; $4688 ?o..n...
			defb $FB,$F6,$36,$ED,$F8,$00,$00,$00				; $4690 ..6.....
			defb $00,$8A,$A0,$00,$00,$00,$00,$00				; $4698 ........
			defb $00,$00,$00,$00,$10,$FF,$FF,$FF				; $46A0 ........
			defb $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF				; $46A8 ........
			defb $FF,$FF,$FF,$FF,$F8,$00,$00,$00				; $46B0 ........
			defb $00,$82,$A0,$00,$00,$00,$00,$00				; $46B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $46C0 ........
			defb $00,$00,$16,$04,$99,$2A,$42,$AA				; $46C8 .....*B.
			defb $A3,$FC,$00,$00,$00,$10,$00,$00				; $46D0 ........
			defb $02,$A8,$A0,$00,$00,$00,$08,$00				; $46D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $46E0 ........
			defb $00,$00,$4B,$40,$AA,$AA,$6F,$FF				; $46E8 ..K@..o.
			defb $F2,$A9,$FE,$00,$00,$00,$00,$00				; $46F0 ........
			defb $2A,$AA,$30,$00,$00,$00,$00,$00				; $46F8 *.0.....
			defb $00,$00,$07,$FC,$7F,$F8,$00,$00				; $4700 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4708 ........
			defb $00,$00,$00,$00,$00,$7F,$E0,$00				; $4710 ........
			defb $00,$18,$E0,$00,$00,$00,$00,$00				; $4718 ........
			defb $0F,$FF,$FE,$0F,$F8,$FF,$FF,$F8				; $4720 ........
			defb $3F,$FF,$87,$FF,$FF,$0F,$FF,$FE				; $4728 ?.......
			defb $1F,$FF,$FC,$3F,$C4,$7F,$00,$00				; $4730 ...?....
			defb $00,$15,$50,$00,$00,$00,$00,$00				; $4738 ..P.....
			defb $00,$01,$F0,$0E,$00,$70,$C0,$30				; $4740 .....p.0
			defb $F8,$00,$3E,$0C,$01,$80,$1F,$18				; $4748 ..>.....
			defb $01,$0E,$30,$06,$1B,$C3,$00,$00				; $4750 ..0.....
			defb $38,$15,$50,$00,$00,$00,$00,$00				; $4758 8.P.....
			defb $00,$00,$0F,$EF,$F7,$FC,$3F,$FF				; $4760 ......?.
			defb $FF,$F8,$03,$FF,$F0,$FF,$03,$FF				; $4768 ........
			defb $FE,$3F,$C7,$FF,$FF,$FF,$00,$00				; $4770 .?......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4778 ........
			defb $00,$00,$00,$0F,$F0,$31,$FB,$00				; $4780 .....1..
			defb $30,$6C,$1F,$8C,$6F,$D8,$01,$FD				; $4788 0l..o...
			defb $9B,$07,$F6,$FD,$80,$00,$00,$00				; $4790 ........
			defb $00,$55,$50,$00,$00,$00,$00,$00				; $4798 .UP.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $47A0 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $47A8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $47B0 ........
			defb $00,$01,$50,$00,$00,$00,$00,$00				; $47B8 ..P.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $47C0 ........
			defb $00,$00,$28,$1B,$64,$55,$79,$55				; $47C8 ..(.dUyU
			defb $53,$FE,$00,$00,$00,$00,$00,$00				; $47D0 S.......
			defb $05,$53,$18,$00,$00,$00,$5D,$00				; $47D8 .S....].
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $47E0 ........
			defb $00,$00,$24,$05,$55,$55,$33,$FF				; $47E8 ..$.UU3.
			defb $F1,$54,$7F,$00,$00,$00,$00,$00				; $47F0 .T......
			defb $15,$55,$40,$00,$00,$00,$00,$00				; $47F8 .U@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4800 ........
			defb $00,$00,$07,$08,$AA,$AA,$6D,$FF				; $4808 ......m.
			defb $F0,$AA,$9F,$00,$00,$00,$00,$08				; $4810 ........
			defb $2A,$A2,$A0,$00,$00,$00,$00,$00				; $4818 *.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4820 ........
			defb $00,$00,$A0,$78,$8A,$AA,$01,$A0				; $4828 ...x....
			defb $80,$00,$02,$AA,$00,$00,$02,$AA				; $4830 ........
			defb $AA,$8A,$A0,$00,$00,$00,$00,$00				; $4838 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4840 ........
			defb $00,$02,$80,$37,$60,$6A,$3F,$C0				; $4848 ...7`j?.
			defb $00,$00,$00,$02,$CA,$00,$2A,$AA				; $4850 ......*.
			defb $A8,$E4,$AA,$E0,$00,$00,$00,$00				; $4858 ........
			defb $00,$00,$00,$00,$00,$00,$00,$01				; $4860 ........
			defb $00,$32,$80,$8D,$01,$FC,$AB,$C0				; $4868 .2......
			defb $00,$00,$00,$00,$BE,$8A,$AA,$AA				; $4870 ........
			defb $A0,$6C,$00,$00,$00,$00,$00,$00				; $4878 .l......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4880 ........
			defb $00,$3C,$80,$26,$00,$7C,$0A,$80				; $4888 .<.&.|..
			defb $10,$00,$00,$00,$AF,$2B,$A2,$AA				; $4890 .....+..
			defb $80,$00,$00,$00,$00,$00,$00,$00				; $4898 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $48A0 ........
			defb $00,$5E,$80,$19,$00,$00,$00,$2A				; $48A8 .^.....*
			defb $00,$00,$00,$00,$2B,$CA,$9A,$E2				; $48B0 ....+...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $48B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $48C0 ........
			defb $00,$0C,$A0,$0A,$80,$00,$00,$02				; $48C8 ........
			defb $A0,$00,$00,$00,$3D,$F2,$71,$A8				; $48D0 ....=.q.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $48D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $48E0 ........
			defb $00,$02,$A8,$11,$80,$00,$00,$00				; $48E8 ........
			defb $2A,$00,$00,$17,$4A,$A7,$DC,$A8				; $48F0 *...J...
			defb $09,$E0,$00,$00,$00,$08,$00,$00				; $48F8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4900 ........
			defb $00,$00,$10,$E4,$55,$55,$3A,$7F				; $4908 ....UU:.
			defb $E0,$15,$41,$80,$00,$00,$00,$14				; $4910 ..A.....
			defb $55,$55,$40,$00,$00,$00,$00,$00				; $4918 UU@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4920 ........
			defb $00,$01,$40,$1E,$45,$54,$02,$83				; $4928 ..@.ET..
			defb $00,$00,$01,$55,$00,$00,$05,$55				; $4930 ...U...U
			defb $55,$15,$40,$00,$00,$00,$00,$00				; $4938 U.@.....
			defb $00,$00,$20,$00,$00,$00,$00,$00				; $4940 .. .....
			defb $00,$01,$40,$9D,$81,$A5,$3F,$E0				; $4948 ..@...?.
			defb $00,$00,$00,$00,$65,$00,$55,$55				; $4950 ....e.UU
			defb $54,$2D,$4A,$A0,$00,$00,$00,$00				; $4958 T-J.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4960 ........
			defb $00,$25,$40,$5C,$03,$FC,$55,$80				; $4968 .%@\..U.
			defb $00,$00,$00,$00,$5E,$45,$55,$55				; $4970 ....^EUU
			defb $50,$6C,$00,$00,$00,$00,$00,$00				; $4978 Pl......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4980 ........
			defb $00,$21,$40,$00,$00,$38,$05,$40				; $4988 .!@..8.@
			defb $00,$00,$00,$00,$4F,$95,$51,$55				; $4990 ....O.QU
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4998 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $49A0 ........
			defb $00,$7E,$40,$00,$00,$00,$00,$54				; $49A8 .~@....T
			defb $00,$00,$00,$00,$13,$D4,$5C,$D4				; $49B0 ......\.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $49B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $49C0 ........
			defb $00,$0D,$50,$00,$00,$00,$02,$01				; $49C8 ..P.....
			defb $50,$00,$00,$00,$DC,$F5,$76,$94				; $49D0 P.....v.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $49D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $49E0 ........
			defb $00,$01,$50,$00,$00,$00,$00,$00				; $49E8 ..P.....
			defb $15,$00,$00,$37,$95,$53,$DD,$54				; $49F0 ...7.S.T
			defb $15,$F0,$00,$00,$00,$00,$00,$00				; $49F8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4A00 ........
			defb $00,$00,$2A,$78,$AA,$AA,$ED,$9F				; $4A08 ..*x....
			defb $E0,$0A,$AA,$00,$00,$00,$00,$28				; $4A10 .......(
			defb $AA,$AA,$A0,$00,$00,$00,$00,$00				; $4A18 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4A20 ........
			defb $00,$00,$A0,$07,$8A,$A8,$0A,$04				; $4A28 ........
			defb $00,$00,$00,$AA,$80,$00,$02,$AA				; $4A30 ........
			defb $AA,$4A,$A0,$00,$00,$00,$00,$00				; $4A38 .J......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4A40 ........
			defb $00,$0A,$80,$57,$02,$E2,$9F,$E0				; $4A48 ...W....
			defb $00,$00,$00,$00,$72,$80,$AA,$AA				; $4A50 ....r...
			defb $A8,$0E,$AF,$E0,$00,$00,$00,$00				; $4A58 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4A60 ........
			defb $00,$0A,$80,$10,$07,$FE,$29,$00				; $4A68 ......).
			defb $00,$00,$00,$00,$9E,$A2,$AA,$AA				; $4A70 ........
			defb $A0,$78,$00,$00,$00,$00,$00,$00				; $4A78 .x......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4A80 ........
			defb $00,$1E,$80,$52,$00,$20,$02,$A0				; $4A88 ...R. ..
			defb $00,$00,$00,$00,$AF,$A9,$26,$AA				; $4A90 ......&.
			defb $80,$00,$00,$00,$00,$00,$00,$00				; $4A98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4AA0 ........
			defb $00,$7E,$A0,$29,$00,$00,$08,$2A				; $4AA8 .~.)...*
			defb $00,$00,$00,$00,$2B,$EA,$9D,$40				; $4AB0 ....+..@
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4AB8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4AC0 ........
			defb $00,$04,$A0,$14,$80,$00,$00,$02				; $4AC8 ........
			defb $A0,$00,$00,$00,$DA,$72,$F7,$28				; $4AD0 .....r.(
			defb $01,$00,$00,$00,$00,$00,$00,$00				; $4AD8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4AE0 ........
			defb $00,$02,$A8,$11,$20,$00,$00,$00				; $4AE8 .... ...
			defb $2A,$80,$00,$7B,$2A,$AD,$BA,$A8				; $4AF0 *..{*...
			defb $09,$F0,$00,$00,$00,$08,$00,$00				; $4AF8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4B00 ........
			defb $00,$00,$45,$1E,$55,$54,$B8,$6F				; $4B08 ..E.UT.o
			defb $C0,$05,$55,$40,$00,$00,$00,$54				; $4B10 ..U@...T
			defb $55,$45,$40,$00,$00,$00,$00,$00				; $4B18 UE@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4B20 ........
			defb $00,$01,$40,$4D,$E5,$54,$28,$08				; $4B28 ..@M.T(.
			defb $00,$00,$00,$55,$40,$00,$05,$55				; $4B30 ...U@..U
			defb $55,$55,$50,$00,$00,$00,$00,$00				; $4B38 UUP.....
			defb $00,$00,$20,$00,$00,$00,$00,$00				; $4B40 .. .....
			defb $00,$15,$40,$9C,$0B,$85,$5F,$F0				; $4B48 ..@..._.
			defb $00,$00,$00,$00,$79,$41,$55,$55				; $4B50 ....yAUU
			defb $50,$0E,$40,$00,$00,$00,$00,$00				; $4B58 P.@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4B60 ........
			defb $00,$05,$40,$40,$0B,$FE,$14,$00				; $4B68 ..@@....
			defb $00,$00,$00,$00,$5E,$51,$15,$55				; $4B70 ....^Q.U
			defb $40,$70,$00,$00,$00,$00,$00,$00				; $4B78 @p......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4B80 ........
			defb $00,$7E,$40,$00,$00,$00,$01,$50				; $4B88 .~@....P
			defb $00,$00,$00,$00,$57,$95,$4F,$55				; $4B90 ....W.OU
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4B98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4BA0 ........
			defb $00,$3E,$40,$00,$00,$00,$00,$15				; $4BA8 .>@.....
			defb $00,$10,$00,$00,$13,$E4,$5D,$94				; $4BB0 ......].
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4BB8 ........
			defb $00,$00,$00,$08,$00,$00,$00,$00				; $4BC0 ........
			defb $00,$01,$50,$00,$00,$00,$00,$01				; $4BC8 ..P.....
			defb $50,$00,$00,$01,$E9,$64,$F7,$54				; $4BD0 P....d.T
			defb $01,$80,$00,$00,$00,$00,$00,$00				; $4BD8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4BE0 ........
			defb $00,$01,$54,$00,$00,$00,$00,$00				; $4BE8 ..T.....
			defb $15,$40,$00,$7A,$55,$4E,$BA,$50				; $4BF0 .@.zUN.P
			defb $15,$F0,$00,$00,$00,$00,$00,$00				; $4BF8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4C00 ........
			defb $00,$00,$28,$A7,$2A,$AA,$EE,$33				; $4C08 ..(.*..3
			defb $C0,$02,$AA,$A0,$00,$00,$00,$28				; $4C10 .......(
			defb $AA,$AA,$A0,$00,$00,$00,$00,$00				; $4C18 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4C20 ........
			defb $00,$00,$80,$97,$72,$A8,$A0,$30				; $4C28 ....r..0
			defb $00,$00,$00,$2A,$A0,$00,$0A,$AA				; $4C30 ...*....
			defb $AA,$48,$AC,$00,$00,$00,$00,$80				; $4C38 .H......
			defb $00,$01,$74,$00,$00,$00,$00,$00				; $4C40 ..t.....
			defb $00,$32,$80,$10,$0E,$02,$AF,$F8				; $4C48 .2......
			defb $00,$00,$00,$00,$BC,$A2,$AA,$AA				; $4C50 ........
			defb $A8,$1A,$A0,$00,$00,$00,$00,$00				; $4C58 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4C60 ........
			defb $00,$0A,$80,$80,$7B,$FF,$2A,$00				; $4C68 ....{.*.
			defb $00,$00,$00,$00,$9F,$24,$8A,$AA				; $4C70 .....$..
			defb $A0,$20,$00,$00,$00,$00,$00,$00				; $4C78 . ......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4C80 ........
			defb $00,$7E,$80,$48,$80,$02,$02,$A0				; $4C88 .~.H....
			defb $00,$00,$00,$00,$A7,$A9,$2F,$0A				; $4C90 ....../.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4C98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4CA0 ........
			defb $00,$3E,$A0,$24,$40,$00,$00,$0A				; $4CA8 .>.$@...
			defb $80,$00,$00,$00,$0B,$EA,$9D,$C8				; $4CB0 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4CB8 ........
			defb $00,$00,$00,$14,$00,$00,$00,$00				; $4CC0 ........
			defb $00,$02,$A0,$0A,$80,$00,$00,$00				; $4CC8 ........
			defb $A8,$00,$00,$03,$F2,$A9,$6F,$28				; $4CD0 ......o(
			defb $01,$C0,$00,$00,$00,$08,$00,$00				; $4CD8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4CE0 ........
			defb $00,$02,$A8,$02,$20,$00,$00,$04				; $4CE8 .... ...
			defb $0A,$80,$00,$BC,$AA,$9F,$3A,$A8				; $4CF0 ......:.
			defb $29,$F0,$00,$00,$00,$00,$00,$00				; $4CF8 ).......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4D00 ........
			defb $00,$00,$56,$51,$95,$54,$B8,$0D				; $4D08 ..VQ.T..
			defb $C0,$00,$55,$50,$00,$00,$00,$55				; $4D10 ..UP...U
			defb $55,$55,$40,$00,$00,$00,$00,$00				; $4D18 UU@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4D20 ........
			defb $00,$01,$40,$0D,$99,$50,$80,$C0				; $4D28 ..@..P..
			defb $00,$00,$00,$14,$50,$00,$15,$55				; $4D30 ....P..U
			defb $54,$D1,$4E,$00,$00,$00,$01,$40				; $4D38 T.N....@
			defb $00,$00,$20,$00,$00,$00,$00,$00				; $4D40 .. .....
			defb $00,$15,$40,$40,$58,$11,$4F,$FC				; $4D48 ..@@X.O.
			defb $00,$00,$00,$00,$3C,$51,$55,$55				; $4D50 ....<QUU
			defb $50,$1A,$40,$00,$00,$00,$00,$00				; $4D58 P.@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4D60 ........
			defb $00,$05,$40,$01,$A1,$FF,$15,$00				; $4D68 ..@.....
			defb $00,$00,$00,$00,$5F,$52,$45,$55				; $4D70 ...._REU
			defb $40,$20,$00,$00,$00,$00,$00,$00				; $4D78 @ ......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4D80 ........
			defb $00,$7E,$40,$00,$00,$00,$01,$50				; $4D88 .~@....P
			defb $00,$00,$00,$00,$57,$D4,$4F,$65				; $4D90 ....W.Oe
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4D98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4DA0 ........
			defb $00,$1D,$40,$00,$00,$00,$00,$15				; $4DA8 ..@.....
			defb $00,$00,$00,$00,$15,$E5,$3D,$90				; $4DB0 ......=.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4DB8 ........
			defb $00,$00,$00,$08,$00,$00,$00,$00				; $4DC0 ........
			defb $00,$01,$50,$00,$00,$00,$00,$00				; $4DC8 ..P.....
			defb $54,$00,$40,$01,$F5,$55,$AF,$54				; $4DD0 T.@..U.T
			defb $05,$C0,$00,$00,$00,$00,$00,$00				; $4DD8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4DE0 ........
			defb $00,$01,$54,$00,$00,$00,$00,$00				; $4DE8 ..T.....
			defb $05,$40,$01,$DD,$55,$5F,$56,$50				; $4DF0 .@..U_VP
			defb $15,$F8,$00,$00,$00,$00,$00,$00				; $4DF8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4E00 ........
			defb $00,$00,$A7,$8A,$2A,$AA,$E0,$16				; $4E08 ....*...
			defb $00,$00,$2A,$A8,$00,$00,$00,$AA				; $4E10 ..*.....
			defb $AA,$8A,$A0,$00,$00,$00,$00,$00				; $4E18 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4E20 ........
			defb $00,$02,$80,$57,$60,$AA,$03,$00				; $4E28 ...W`...
			defb $00,$00,$00,$0A,$28,$00,$2A,$AA				; $4E30 ....(.*.
			defb $AA,$A8,$AB,$80,$00,$00,$00,$80				; $4E38 ........
			defb $00,$00,$00,$00,$00,$00,$00,$01				; $4E40 ........
			defb $00,$12,$80,$81,$70,$38,$A7,$F8				; $4E48 ....p8..
			defb $00,$00,$00,$00,$BD,$28,$AA,$AA				; $4E50 .....(..
			defb $A0,$3A,$A0,$00,$00,$00,$00,$00				; $4E58 .:......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4E60 ........
			defb $00,$1A,$80,$46,$C0,$FF,$0A,$80				; $4E68 ...F....
			defb $80,$00,$00,$00,$AF,$2B,$22,$AA				; $4E70 .....+".
			defb $80,$00,$00,$00,$00,$00,$00,$00				; $4E78 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4E80 ........
			defb $00,$78,$80,$28,$80,$00,$00,$A8				; $4E88 .x.(....
			defb $01,$00,$00,$00,$A7,$CA,$2E,$EA				; $4E90 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4E98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4EA0 ........
			defb $00,$1C,$A0,$14,$40,$00,$00,$0A				; $4EA8 ....@...
			defb $80,$00,$00,$00,$19,$E8,$9B,$A8				; $4EB0 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4EB8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4EC0 ........
			defb $00,$02,$A8,$02,$80,$00,$00,$00				; $4EC8 ........
			defb $AA,$00,$00,$06,$EA,$AB,$C6,$A8				; $4ED0 ........
			defb $01,$E0,$00,$00,$00,$08,$00,$00				; $4ED8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4EE0 ........
			defb $00,$02,$A8,$00,$A0,$00,$00,$00				; $4EE8 ........
			defb $02,$A0,$03,$DA,$AA,$8E,$E7,$28				; $4EF0 .......(
			defb $2A,$F8,$00,$00,$00,$00,$00,$00				; $4EF8 *.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4F00 ........
			defb $00,$00,$51,$E5,$55,$54,$80,$79				; $4F08 ..Q.UT.y
			defb $80,$00,$15,$54,$00,$00,$01,$55				; $4F10 ...T...U
			defb $55,$15,$40,$00,$00,$00,$00,$00				; $4F18 U.@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4F20 ........
			defb $00,$01,$40,$9D,$D8,$15,$0F,$80				; $4F28 ..@.....
			defb $00,$00,$00,$04,$94,$00,$55,$55				; $4F30 ......UU
			defb $54,$A5,$4B,$E0,$00,$00,$00,$00				; $4F38 T.K.....
			defb $00,$00,$20,$00,$00,$00,$00,$02				; $4F40 .. .....
			defb $80,$25,$40,$03,$C0,$F9,$57,$F0				; $4F48 .%@...W.
			defb $00,$00,$00,$00,$3E,$15,$55,$55				; $4F50 ....>.UU
			defb $50,$2E,$00,$00,$00,$00,$00,$00				; $4F58 P.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4F60 ........
			defb $00,$19,$40,$8B,$00,$FE,$05,$00				; $4F68 ..@.....
			defb $00,$00,$00,$00,$4F,$53,$95,$55				; $4F70 ....OS.U
			defb $40,$00,$00,$00,$00,$00,$00,$00				; $4F78 @.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4F80 ........
			defb $00,$26,$40,$00,$00,$00,$00,$54				; $4F88 .&@....T
			defb $00,$00,$00,$00,$57,$D4,$46,$E4				; $4F90 ....W.F.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4F98 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4FA0 ........
			defb $00,$1D,$50,$00,$00,$00,$00,$05				; $4FA8 ..P.....
			defb $40,$01,$00,$00,$39,$F1,$2B,$90				; $4FB0 @...9.+.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4FB8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4FC0 ........
			defb $00,$01,$50,$00,$00,$00,$00,$00				; $4FC8 ..P.....
			defb $54,$00,$00,$0E,$D5,$53,$DA,$54				; $4FD0 T....S.T
			defb $05,$E0,$00,$00,$00,$5D,$00,$00				; $4FD8 .....]..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $4FE0 ........
			defb $00,$01,$54,$00,$00,$00,$00,$00				; $4FE8 ..T.....
			defb $05,$50,$05,$E5,$55,$56,$E3,$50				; $4FF0 .P..UV.P
			defb $54,$F8,$00,$00,$00,$00,$00,$00				; $4FF8 T.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5000 ........
			defb $00,$00,$AA,$04,$40,$00,$00,$00				; $5008 ....@...
			defb $02,$A8,$0E,$EA,$AA,$89,$EB,$20				; $5010 ....... 
			defb $AA,$F8,$00,$00,$00,$00,$00,$00				; $5018 ........
			defb $0D,$2A,$A0,$00,$00,$00,$00,$00				; $5020 .*......
			defb $00,$00,$2A,$01,$10,$20,$2A,$00				; $5028 ..*.. *.
			defb $0E,$2A,$AA,$AA,$AA,$A2,$A6,$85				; $5030 .*......
			defb $AA,$80,$00,$00,$00,$00,$00,$00				; $5038 ........
			defb $00,$04,$AA,$AA,$00,$00,$00,$00				; $5040 ........
			defb $00,$00,$0A,$A0,$54,$00,$2A,$AA				; $5048 ....T.*.
			defb $A9,$E2,$AA,$AA,$AA,$9B,$D2,$7F				; $5050 ........
			defb $E0,$00,$00,$00,$00,$00,$00,$00				; $5058 ........
			defb $2F,$B8,$00,$AA,$AA,$AA,$A0,$02				; $5060 /.......
			defb $AA,$AA,$A8,$AA,$08,$90,$2A,$8F				; $5068 ......*.
			defb $FF,$08,$AA,$A9,$FF,$CA,$AA,$A0				; $5070 ........
			defb $00,$00,$00,$00,$20,$00,$00,$00				; $5078 .... ...
			defb $00,$00,$00,$00,$29,$DC,$AA,$AA				; $5080 ....)...
			defb $AA,$AA,$AA,$A0,$7D,$F0,$02,$3F				; $5088 ....}..?
			defb $2A,$2A,$32,$AA,$7F,$FC,$A8,$00				; $5090 **2.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5098 ........
			defb $00,$00,$00,$00,$00,$00,$02,$AA				; $50A0 ........
			defb $A3,$00,$0A,$AA,$0A,$AA,$6A,$BA				; $50A8 ......j.
			defb $A3,$FF,$FF,$27,$F2,$2A,$00,$00				; $50B0 ...'.*..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $50B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$3F				; $50C0 .......?
			defb $8A,$80,$00,$00,$0A,$A8,$0A,$9F				; $50C8 ........
			defb $82,$AF,$CA,$A8,$3F,$FE,$3F,$FE				; $50D0 ....?.?.
			defb $3F,$FE,$3F,$FE,$3F,$FE,$3F,$FE				; $50D8 ?.?.?.?.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $50E0 ........
			defb $00,$00,$00,$3F,$CA,$AA,$AA,$AA				; $50E8 ...?....
			defb $AA,$AA,$AA,$00,$63,$E3,$63,$FF				; $50F0 ....c.c.
			defb $66,$33,$7F,$E3,$63,$E3,$63,$83				; $50F8 f3..c.c.
			defb $01,$80,$00,$00,$00,$00,$00,$00				; $5100 ........
			defb $00,$01,$54,$00,$00,$00,$00,$7C				; $5108 ..T....|
			defb $01,$50,$0E,$D5,$55,$45,$D3,$50				; $5110 .P..UE.P
			defb $54,$F8,$00,$00,$00,$00,$00,$00				; $5118 T.......
			defb $07,$55,$50,$00,$00,$00,$00,$00				; $5120 .UP.....
			defb $00,$00,$55,$00,$00,$30,$44,$00				; $5128 ..U..0D.
			defb $0E,$15,$55,$55,$55,$55,$0D,$0D				; $5130 ..UUUU..
			defb $D5,$40,$00,$00,$00,$00,$00,$00				; $5138 .@......
			defb $00,$02,$55,$55,$40,$00,$00,$00				; $5140 ..UU@...
			defb $00,$00,$05,$40,$00,$03,$85,$55				; $5148 ...@...U
			defb $54,$E1,$55,$55,$55,$3D,$D5,$7F				; $5150 T.UUU=..
			defb $C0,$00,$00,$00,$00,$00,$00,$00				; $5158 ........
			defb $2A,$A0,$00,$15,$55,$55,$50,$55				; $5160 *...UUPU
			defb $55,$55,$54,$55,$40,$00,$15,$51				; $5168 UUTU@..Q
			defb $FF,$E5,$55,$53,$FF,$95,$45,$40				; $5170 ..US..E@
			defb $00,$00,$00,$00,$50,$00,$00,$00				; $5178 ....P...
			defb $00,$00,$00,$00,$13,$6F,$15,$55				; $5180 .....o.U
			defb $55,$55,$55,$4F,$1D,$F7,$DD,$4F				; $5188 UUUO...O
			defb $C4,$54,$F9,$55,$7F,$F9,$50,$00				; $5190 .T.U..P.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5198 ........
			defb $00,$00,$00,$00,$00,$00,$00,$55				; $51A0 .......U
			defb $54,$00,$01,$55,$45,$55,$35,$5D				; $51A8 T..UEU5]
			defb $1F,$FF,$FF,$57,$F9,$54,$00,$00				; $51B0 ...W.T..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $51B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$18				; $51C0 ........
			defb $05,$40,$00,$00,$15,$54,$00,$00				; $51C8 .@...T..
			defb $55,$5F,$95,$52,$7F,$FF,$7F,$FF				; $51D0 U_.R....
			defb $7F,$FF,$7F,$FF,$7F,$FF,$7F,$FF				; $51D8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $51E0 ........
			defb $00,$00,$00,$1F,$D5,$55,$55,$55				; $51E8 .....UUU
			defb $55,$55,$40,$00,$63,$E3,$63,$FF				; $51F0 UU@.c.c.
			defb $66,$33,$7F,$E3,$63,$E3,$63,$C3				; $51F8 f3..c.c.
			defb $1F,$E0,$00,$00,$00,$00,$00,$01				; $5200 ........
			defb $00,$00,$AA,$04,$48,$00,$01,$83				; $5208 ....H...
			defb $E0,$A8,$2F,$2A,$AA,$AA,$CB,$20				; $5210 ../*... 
			defb $AA,$F8,$00,$00,$00,$00,$00,$00				; $5218 ........
			defb $06,$AA,$AA,$00,$00,$00,$00,$00				; $5220 ........
			defb $00,$00,$2A,$81,$12,$1C,$48,$00				; $5228 ..*...H.
			defb $0F,$0A,$AA,$AA,$AA,$A2,$AD,$0D				; $5230 ........
			defb $E2,$80,$00,$00,$00,$00,$00,$00				; $5238 ........
			defb $00,$01,$AA,$AA,$A0,$00,$00,$00				; $5240 ........
			defb $00,$00,$AA,$A0,$45,$C1,$F8,$AA				; $5248 ....E...
			defb $AA,$E0,$AA,$AA,$AA,$7C,$0A,$7F				; $5250 .....|..
			defb $C0,$00,$00,$00,$00,$00,$00,$00				; $5258 ........
			defb $6A,$A0,$00,$0A,$A8,$AA,$AA,$AA				; $5260 j.......
			defb $AA,$AA,$AA,$2A,$A1,$00,$02,$AA				; $5268 ...*....
			defb $3F,$EA,$2A,$A7,$FF,$AA,$8A,$A0				; $5270 ?.*.....
			defb $00,$00,$00,$00,$20,$00,$00,$00				; $5278 .... ...
			defb $00,$00,$00,$00,$0E,$B6,$A0,$2A				; $5280 .......*
			defb $AA,$AA,$AA,$AF,$A0,$F7,$DC,$A3				; $5288 ........
			defb $C2,$A3,$FC,$AA,$00,$02,$A0,$00				; $5290 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5298 ........
			defb $00,$00,$00,$00,$00,$00,$00,$2A				; $52A0 .......*
			defb $AA,$00,$00,$0A,$A2,$AA,$9A,$AC				; $52A8 ........
			defb $BF,$FE,$7E,$27,$FC,$AA,$00,$00				; $52B0 ..~'....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $52B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $52C0 ........
			defb $0A,$80,$00,$00,$00,$AA,$80,$07				; $52C8 ........
			defb $AA,$9F,$AA,$8B,$63,$E3,$60,$03				; $52D0 ....c.`.
			defb $66,$33,$60,$03,$60,$03,$63,$E3				; $52D8 f3`.`.c.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $52E0 ........
			defb $00,$00,$00,$1F,$EA,$AA,$AA,$AA				; $52E8 ........
			defb $AA,$A8,$00,$00,$63,$E3,$63,$FF				; $52F0 ....c.c.
			defb $66,$33,$63,$E3,$63,$E3,$63,$E3				; $52F8 f3c.c.c.
			defb $6C,$50,$00,$00,$00,$00,$00,$02				; $5300 lP......
			defb $80,$00,$54,$00,$00,$00,$06,$24				; $5308 ..T....$
			defb $11,$54,$77,$15,$55,$45,$15,$80				; $5310 .Tw.UE..
			defb $55,$78,$00,$00,$00,$00,$00,$00				; $5318 Ux......
			defb $01,$55,$55,$00,$00,$00,$00,$00				; $5320 .UU.....
			defb $00,$00,$15,$00,$00,$1F,$80,$03				; $5328 ........
			defb $FF,$05,$55,$55,$55,$55,$1A,$1D				; $5330 ..UUUU..
			defb $F8,$00,$00,$00,$00,$00,$00,$00				; $5338 ........
			defb $00,$00,$45,$55,$50,$00,$00,$00				; $5340 ..EUP...
			defb $00,$15,$45,$50,$00,$01,$FF,$15				; $5348 ..EP....
			defb $54,$C0,$55,$55,$54,$FD,$C4,$3F				; $5350 T.UUT..?
			defb $80,$00,$00,$00,$00,$00,$00,$00				; $5358 ........
			defb $00,$00,$00,$05,$54,$55,$55,$55				; $5360 ....TUUU
			defb $55,$55,$55,$15,$54,$00,$09,$55				; $5368 UUU.T..U
			defb $4F,$F5,$55,$53,$FF,$55,$15,$40				; $5370 O.US.U.@
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5378 ........
			defb $00,$00,$00,$00,$01,$DC,$55,$01				; $5380 ......U.
			defb $55,$55,$55,$43,$BE,$03,$DE,$00				; $5388 UUUC....
			defb $15,$53,$FC,$55,$55,$55,$50,$00				; $5390 .S.UUUP.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5398 ........
			defb $00,$00,$00,$00,$00,$00,$00,$15				; $53A0 ........
			defb $55,$00,$00,$01,$51,$55,$4C,$4E				; $53A8 U...QULN
			defb $7F,$F8,$FC,$57,$FD,$54,$00,$00				; $53B0 ...W.T..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $53B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $53C0 ........
			defb $05,$00,$00,$00,$00,$15,$55,$50				; $53C8 ......UP
			defb $55,$3F,$54,$54,$63,$E3,$60,$03				; $53D0 U?TTc.`.
			defb $66,$33,$60,$03,$60,$03,$61,$E3				; $53D8 f3`.`.a.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $53E0 ........
			defb $00,$00,$00,$0F,$E5,$55,$55,$55				; $53E8 .....UUU
			defb $55,$40,$00,$00,$63,$E3,$63,$FF				; $53F0 U@..c.c.
			defb $66,$33,$63,$E3,$63,$E3,$63,$E3				; $53F8 f3c.c.c.
			defb $1B,$48,$00,$00,$00,$00,$00,$01				; $5400 .H......
			defb $00,$00,$AA,$00,$A8,$00,$08,$A0				; $5408 ........
			defb $58,$AA,$F6,$AA,$AA,$AA,$A9,$A0				; $5410 X.......
			defb $AA,$70,$00,$00,$00,$00,$00,$00				; $5418 .p......
			defb $01,$AA,$AA,$80,$00,$00,$00,$00				; $5420 ........
			defb $00,$00,$2A,$80,$22,$03,$F8,$1F				; $5428 ..*."...
			defb $FE,$0A,$AA,$AA,$AA,$A0,$3A,$9E				; $5430 ......:.
			defb $FE,$00,$00,$00,$00,$00,$00,$00				; $5438 ........
			defb $00,$00,$32,$AA,$A8,$00,$00,$AA				; $5440 ..2.....
			defb $AA,$AA,$A2,$A0,$01,$40,$7F,$E2				; $5448 .....@..
			defb $AA,$40,$AA,$AA,$89,$FA,$48,$BF				; $5450 .@....H.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5458 ........
			defb $00,$00,$00,$02,$AA,$2A,$AA,$AA				; $5460 .....*..
			defb $AA,$AA,$AA,$82,$AA,$A0,$0E,$2A				; $5468 .......*
			defb $A1,$F2,$2A,$AB,$FE,$00,$2A,$80				; $5470 ..*...*.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5478 ........
			defb $00,$00,$00,$00,$00,$B8,$AA,$A8				; $5480 ........
			defb $2A,$AA,$AA,$A4,$3E,$F8,$00,$2A				; $5488 *...>..*
			defb $AA,$AB,$FE,$2A,$AA,$AA,$A0,$00				; $5490 ...*....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5498 ........
			defb $00,$00,$00,$00,$00,$00,$00,$62				; $54A0 .......b
			defb $AA,$A0,$00,$00,$28,$2A,$A7,$27				; $54A8 ....(*.'
			defb $7F,$C1,$F8,$A7,$FE,$A8,$00,$00				; $54B0 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $54B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $54C0 ........
			defb $00,$00,$00,$00,$00,$02,$AA,$2A				; $54C8 .......*
			defb $AA,$7E,$02,$AA,$63,$E3,$63,$FF				; $54D0 .~..c.c.
			defb $66,$33,$63,$E3,$63,$E3,$60,$E3				; $54D8 f3c.c.`.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $54E0 ........
			defb $00,$00,$00,$07,$F2,$AA,$AA,$AA				; $54E8 ........
			defb $AA,$80,$00,$00,$63,$E3,$60,$03				; $54F0 ....c.`.
			defb $60,$03,$60,$03,$60,$03,$63,$E3				; $54F8 `.`.`.c.
			defb $3C,$D4,$00,$00,$00,$00,$00,$00				; $5500 <.......
			defb $00,$00,$55,$00,$00,$00,$12,$1F				; $5508 ..U.....
			defb $38,$55,$79,$55,$55,$45,$15,$41				; $5510 8UyUUE.A
			defb $55,$30,$00,$00,$00,$00,$00,$00				; $5518 U0......
			defb $00,$55,$55,$50,$00,$00,$00,$00				; $5520 .UUP....
			defb $00,$00,$15,$40,$00,$04,$3F,$80				; $5528 ...@..?.
			defb $02,$05,$55,$55,$55,$53,$D5,$3E				; $5530 ..UUUS.>
			defb $FC,$00,$00,$00,$00,$00,$00,$00				; $5538 ........
			defb $00,$00,$09,$55,$55,$00,$00,$55				; $5540 ...UU..U
			defb $55,$55,$51,$50,$00,$00,$0F,$FC				; $5548 UUQP....
			defb $55,$01,$55,$55,$25,$F5,$15,$40				; $5550 U.UU%..@
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5558 ........
			defb $00,$00,$00,$00,$55,$55,$55,$55				; $5560 ....UUUU
			defb $55,$55,$55,$58,$55,$55,$57,$95				; $5568 UUUXUUW.
			defb $54,$39,$15,$51,$FF,$FE,$55,$00				; $5570 T9.Q..U.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5578 ........
			defb $00,$00,$00,$00,$00,$14,$55,$54				; $5580 ......UT
			defb $41,$55,$55,$51,$40,$FB,$EF,$55				; $5588 AUUQ@..U
			defb $55,$45,$FF,$55,$55,$55,$40,$00				; $5590 UE.UUU@.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5598 ........
			defb $00,$00,$00,$00,$00,$00,$00,$F9				; $55A0 ........
			defb $55,$50,$00,$00,$54,$15,$51,$CA				; $55A8 UP..T.Q.
			defb $3F,$15,$F1,$57,$FF,$50,$00,$00				; $55B0 ?..W.P..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $55B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $55C0 ........
			defb $00,$00,$00,$00,$00,$00,$55,$15				; $55C8 ......U.
			defb $55,$01,$55,$55,$63,$E3,$63,$FF				; $55D0 U.UUc.c.
			defb $66,$33,$63,$FF,$63,$E3,$60,$63				; $55D8 f3c.c.`c
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $55E0 ........
			defb $00,$00,$00,$01,$F5,$55,$55,$55				; $55E8 .....UUU
			defb $54,$00,$00,$00,$63,$E3,$60,$03				; $55F0 T...c.`.
			defb $60,$03,$60,$03,$60,$03,$63,$E3				; $55F8 `.`.`.c.
			defb $16,$AA,$80,$00,$00,$00,$00,$00				; $5600 ........
			defb $00,$00,$AA,$00,$28,$00,$10,$E0				; $5608 ....(...
			defb $FC,$2A,$7A,$AA,$AA,$A2,$AB,$41				; $5610 .*z....A
			defb $2A,$B0,$00,$00,$00,$00,$00,$00				; $5618 *.......
			defb $00,$22,$AA,$A8,$00,$00,$00,$00				; $5620 ."......
			defb $00,$00,$2A,$80,$02,$02,$81,$FF				; $5628 ..*.....
			defb $F8,$02,$AA,$AA,$AA,$AB,$D4,$BF				; $5630 ........
			defb $78,$00,$00,$00,$00,$00,$00,$00				; $5638 x.......
			defb $7F,$F8,$06,$AA,$A8,$00,$00,$2A				; $5640 .......*
			defb $AA,$AA,$AA,$A8,$0A,$80,$23,$FF				; $5648 ......#.
			defb $82,$80,$AA,$AA,$6B,$F2,$AA,$A8				; $5650 ....k...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5658 ........
			defb $00,$00,$00,$00,$2A,$22,$AA,$AA				; $5660 ....*"..
			defb $AA,$AA,$AA,$9F,$02,$AA,$A3,$E2				; $5668 ........
			defb $AA,$82,$AA,$A9,$FF,$FE,$AA,$00				; $5670 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5678 ........
			defb $00,$00,$00,$00,$00,$08,$2A,$AA				; $5680 ......*.
			defb $BE,$0A,$AA,$A8,$AA,$01,$EF,$6A				; $5688 .......j
			defb $AA,$1D,$FF,$28,$2A,$8A,$80,$00				; $5690 ...(*...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5698 ........
			defb $00,$00,$00,$00,$00,$00,$01,$FE				; $56A0 ........
			defb $2A,$A0,$00,$00,$2A,$82,$AA,$F1				; $56A8 *...*...
			defb $DC,$AB,$F2,$A7,$FF,$20,$00,$00				; $56B0 ..... ..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $56B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $56C0 ........
			defb $00,$00,$00,$3E,$68,$00,$0A,$A2				; $56C8 ...>h...
			defb $A0,$AA,$AA,$AA,$60,$03,$60,$03				; $56D0 ....`.`.
			defb $66,$33,$60,$03,$63,$E3,$62,$23				; $56D8 f3`.c.b#
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $56E0 ........
			defb $00,$00,$00,$00,$F2,$AA,$AA,$AA				; $56E8 ........
			defb $A0,$00,$00,$00,$7F,$FF,$7F,$FF				; $56F0 ........
			defb $7F,$FF,$7F,$FF,$7F,$FF,$7F,$FF				; $56F8 ........
			defb $09,$95,$40,$00,$00,$00,$00,$00				; $5700 ..@.....
			defb $00,$00,$55,$00,$00,$40,$29,$00				; $5708 ..U..@).
			defb $1C,$55,$35,$55,$55,$55,$12,$85				; $5710 .U5UUU..
			defb $95,$40,$00,$00,$00,$00,$00,$00				; $5718 .@......
			defb $00,$19,$55,$54,$00,$00,$00,$00				; $5720 ..UT....
			defb $00,$00,$15,$40,$00,$01,$54,$00				; $5728 ...@..T.
			defb $03,$01,$55,$55,$55,$4B,$C9,$7F				; $5730 ..UUUK..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5738 ........
			defb $20,$08,$01,$55,$51,$55,$40,$05				; $5740  ..UQU@.
			defb $55,$55,$51,$54,$00,$00,$14,$7F				; $5748 UUQT....
			defb $F8,$55,$55,$54,$F7,$E5,$55,$50				; $5750 .UUT..UP
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5758 ........
			defb $00,$00,$00,$00,$14,$F1,$55,$55				; $5760 ......UU
			defb $55,$55,$55,$4F,$7C,$05,$50,$FC				; $5768 UUUO|.P.
			defb $55,$55,$05,$54,$FF,$FD,$54,$00				; $5770 UU.T..T.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5778 ........
			defb $00,$00,$00,$00,$00,$00,$05,$55				; $5780 .......U
			defb $4F,$01,$55,$54,$15,$54,$00,$75				; $5788 O.UT.T.u
			defb $54,$FF,$FF,$13,$C5,$15,$00,$00				; $5790 T.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $5798 ........
			defb $00,$00,$00,$00,$00,$00,$00,$FF				; $57A0 ........
			defb $95,$40,$00,$00,$15,$41,$55,$7F				; $57A8 .@...AU.
			defb $E9,$57,$E5,$57,$00,$00,$00,$00				; $57B0 .W.W....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $57B8 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $57C0 ........
			defb $00,$00,$00,$3F,$95,$55,$50,$00				; $57C8 ...?.UP.
			defb $15,$55,$55,$54,$60,$03,$60,$03				; $57D0 .UUT`.`.
			defb $66,$33,$60,$03,$63,$E3,$63,$03				; $57D8 f3`.c.c.
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $57E0 ........
			defb $00,$00,$00,$00,$35,$55,$55,$55				; $57E8 ....5UUU
			defb $00,$00,$00,$00,$3F,$FE,$3F,$FE				; $57F0 ....?.?.
			defb $3F,$FE,$3F,$FE,$3F,$FE,$3F,$FE				; $57F8 ?.?.?.?.
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5800 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5808 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5810 GGGGGGGG
			defb $47,$46,$46,$46,$46,$46,$46,$46				; $5818 GFFFFFFF
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5820 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5828 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5830 GGGGGGGG
			defb $47,$46,$46,$46,$46,$46,$46,$46				; $5838 GFFFFFFF
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5840 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5848 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5850 GGGGGGGG
			defb $47,$46,$46,$46,$46,$46,$46,$46				; $5858 GFFFFFFF
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5860 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5868 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5870 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5878 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5880 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5888 DDDDDDDD
			defb $44,$44,$44,$44,$44,$44,$44,$44				; $5890 DDDDDDDD
			defb $44,$46,$46,$46,$46,$46,$46,$46				; $5898 DFFFFFFF
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $58A0 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $58A8 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $58B0 GGGGGGGG
			defb $47,$46,$46,$46,$46,$46,$46,$46				; $58B8 GFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $58C0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $58C8 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $58D0 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $58D8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $58E0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $58E8 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $58F0 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $58F8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5900 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $5908 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5910 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5918 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5920 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $5928 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5930 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5938 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5940 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $5948 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5950 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5958 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5960 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $5968 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5970 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5978 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5980 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$46,$46				; $5988 EEEEEEFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5990 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5998 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $59A0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$46				; $59A8 EEEEEEEF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59B0 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59B8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $59C0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$46				; $59C8 EEEEEEEF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59D0 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59D8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $59E0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $59E8 EEEEEEEE
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59F0 FFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $59F8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A00 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A08 EEEEEEEE
			defb $45,$46,$46,$46,$46,$46,$46,$46				; $5A10 EFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5A18 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A20 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A28 EEEEEEEE
			defb $45,$46,$46,$46,$46,$46,$46,$46				; $5A30 EFFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5A38 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A40 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A48 EEEEEEEE
			defb $45,$45,$46,$46,$46,$46,$46,$46				; $5A50 EEFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5A58 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A60 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A68 EEEEEEEE
			defb $45,$45,$46,$46,$46,$46,$46,$46				; $5A70 EEFFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5A78 FFFFFFFF
			defb $45,$46,$46,$46,$45,$45,$45,$45				; $5A80 EFFFEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5A88 EEEEEEEE
			defb $45,$45,$45,$46,$46,$46,$46,$46				; $5A90 EEEFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5A98 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AA0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AA8 EEEEEEEE
			defb $45,$45,$45,$46,$46,$46,$46,$46				; $5AB0 EEEFFFFF
			defb $46,$46,$46,$46,$46,$46,$46,$46				; $5AB8 FFFFFFFF
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AC0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AC8 EEEEEEEE
			defb $45,$45,$45,$45,$47,$47,$47,$47				; $5AD0 EEEEGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5AD8 GGGGGGGG
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AE0 EEEEEEEE
			defb $45,$45,$45,$45,$45,$45,$45,$45				; $5AE8 EEEEEEEE
			defb $45,$45,$45,$45,$47,$47,$47,$47				; $5AF0 EEEEGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $5AF8 GGGGGGGG

CODE1END equ $ ; Marker to show end of object code


; . . . . . . . . . . .
; Code Block 2 Header


#code				0,17,HEADERFLAG	; declare code segment

CODE2START	equ $6103			; 24835

			defb 3              ; Code
			defm "cybernoid "	; Pad to 10 chars
			defw CODE2END-CODE2START	; Length 4405 $1135
			defw CODE2START		; Code Start Address
			defw 0              ; Unused


; . . . . . . . . . . .
; Code Block 2 Data


#code				CODE2START,$9F00,DATAFLAG	; declare code segment

			defb $3E,$00,$00,$00,$00,$00,$00,$00				; $6103 >.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $610B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6113 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $611B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6123 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $612B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6133 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $613B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6143 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $614B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6153 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $615B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6163 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $616B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6173 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $617B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6183 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $618B ........
			defb $00,$00,$00,$00,$00,$00,$00,$DB				; $6193 ........
			defb $02,$4D,$00,$04,$65,$00,$00,$03				; $619B .M..e...
			defb $65,$28,$FE,$1C,$5B,$00,$00,$00				; $61A3 e(..[...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61AB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61B3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61BB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61C3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61CB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61D3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61DB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61E3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61EB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61F3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $61FB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6203 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $620B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6213 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $621B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6223 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $622B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6233 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $623B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6243 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $624B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6253 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $625B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6263 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $626B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6273 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $627B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6283 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $628B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6293 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $629B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62A3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62AB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62B3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62BB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62C3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62CB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62D3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62DB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62E3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62EB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62F3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $62FB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6303 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $630B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6313 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $631B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6323 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $632B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6333 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $633B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6343 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $634B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6353 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $635B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6363 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $636B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6373 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $637B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6383 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $638B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6393 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $639B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63A3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63AB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63B3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63BB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63C3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63CB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63D3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63DB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63E3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63EB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63F3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $63FB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6403 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $640B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6413 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $641B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6423 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $642B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6433 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $643B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6443 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $644B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6453 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $645B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6463 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $646B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6473 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $647B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6483 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $648B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $6493 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $649B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64A3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64AB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64B3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64BB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64C3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64CB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64D3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64DB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64E3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64EB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64F3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $64FB ........
			defb $00                                            ; $6503 .

			DI                      ; $6504                     ; :dbolli:20141209 15:16:27 <<< Main Entry Point
			LD SP,$0000				; $6505
			CALL $70B4				; $6508
			LD HL,$5B00				; $650B
			LD DE,$5B01				; $650E
			LD (HL),$00				; $6511
			LD BC,$0A03				; $6513
			LDIR				; $6516
			XOR A				; $6518
			LD ($81A5),A				; $6519
			LD ($7BFA),A				; $651C
			OUT ($FE),A				; $651F
			LD A,($386E)				; $6521
			SUB $FF				; $6524
			JR Z,$652A				; $6526
			LD A,$01				; $6528
			LD ($8599),A				; $652A
			OR A				; $652D
			JP NZ,$653F				; $652E
			LD A,$C9				; $6531
			LD ($EF38),A				; $6533
			LD ($EF3B),A				; $6536
			LD ($EF42),A				; $6539
			CALL $8738				; $653C
			CALL $EF3B				; $653F
			LD E,$01				; $6542
			CALL $EF42				; $6544
			EI				; $6547
			CALL $7BFC				; $6548
			CALL $671E				; $654B
			CALL $81A6				; $654E
			CALL $66E7				; $6551
			LD A,$04				; $6554
			LD ($78D3),A				; $6556
			LD HL,$78F9				; $6559
			LD DE,$78FA				; $655C
			LD BC,$0005				; $655F
			LD (HL),$30				; $6562
			LDIR				; $6564
			XOR A				; $6566
			CALL $7465				; $6567
			LD A,($711A)				; $656A
			CP $02				; $656D
			JR NC,$6574				; $656F
			JP $656A				; $6571

			XOR A				; $6574
			LD ($711A),A				; $6575
			LD HL,$65EA				; $6578
			INC (HL)				; $657B
			LD HL,$8E44				; $657C
			LD (HL),$FF				; $657F
			LD ($8E42),HL				; $6581
			CALL $688F				; $6584
			CALL $68AC				; $6587
			CALL $75B8				; $658A
			CALL $7A50				; $658D
			CALL $7AEA				; $6590
			CALL $7CD2				; $6593
			CALL $7B4A				; $6596
			CALL $7C10				; $6599
			CALL $7069				; $659C
			CALL $87AC				; $659F
			CALL $8A69				; $65A2
			CALL $7F1D				; $65A5
			CALL $7FE7				; $65A8
			CALL $8ADA				; $65AB
			CALL $89D7				; $65AE
			CALL $8923				; $65B1
			CALL $7584				; $65B4
			CALL $8D3C				; $65B7
			CALL $8CD7				; $65BA
			CALL $9040				; $65BD
			CALL $9267				; $65C0
			CALL $97F0				; $65C3
			CALL $970F				; $65C6
			CALL $9BE2				; $65C9
			CALL $98B4				; $65CC
			CALL $A04E				; $65CF
			CALL $8066				; $65D2
			CALL $A086				; $65D5
			CALL $8EDB				; $65D8
			CALL $7E27				; $65DB
			CALL $79FE				; $65DE
			CALL $660B				; $65E1
			CALL $65EB				; $65E4
			JP $656A				; $65E7

			NOP				; $65EA

			LD A,$FE				; $65EB
			IN A,($FE)				; $65ED
			AND $01				; $65EF
			RET NZ				; $65F1
			LD A,$7F				; $65F2
			IN A,($FE)				; $65F4
			AND $02				; $65F6
			RET NZ				; $65F8
			LD BC,$01F4				; $65F9
			CALL $676A				; $65FC
			CALL $6672				; $65FF
			JR NZ,$65FF				; $6602
			CALL $662B				; $6604
			OR A				; $6607
			JR Z,$6604				; $6608
			RET				; $660A

			LD A,$F7				; $660B
			IN A,($FE)				; $660D
			AND $1F				; $660F
			RET NZ				; $6611
			JP $6504				; $6612

			defb $E6,$F1,$C2,$DF,$0A,$0A                        ; $6615 ......
			defb $E0,$45,$57,$48,$49,$43,$48,$20				; $661B .EWHICH 
			defb $4C,$45,$56,$45,$4C,$20,$3F,$FF				; $6623 LEVEL ?.
			defb $C5,$E5,$21,$49,$66,$16,$FE,$7A				; $662B ..!If..z
			defb $DB,$FE,$1E,$01,$06,$05,$0F,$30				; $6633 .......0
			defb $09,$23,$CB,$23,$10,$F8,$CB,$02				; $663B .#.#....
			defb $38,$ED,$7E,$E1,$C1,$C9,$01,$5A				; $6643 8.~....Z
			defb $58,$43,$56,$41,$53,$44,$46,$47				; $664B XCVASDFG
			defb $51,$57,$45,$52,$54,$31,$32,$33				; $6653 QWERT123
			defb $34,$35,$30,$39,$38,$37,$36,$50				; $665B 4509876P
			defb $4F,$49,$55,$59,$0D,$4C,$4B,$4A				; $6663 OIUY.LKJ
			defb $48,$20,$02,$4D,$4E,$42,$00                    ; $666B H .MNB.

			XOR A				; $6672
			IN A,($FE)				; $6673
			CPL				; $6675
			AND $1F				; $6676
			RET				; $6678

			PUSH HL				; $6679
			PUSH DE				; $667A
			PUSH BC				; $667B
			INC A				; $667C
			PUSH AF				; $667D
			LD HL,($66A8)				; $667E
			LD DE,$0007				; $6681
			ADD HL,DE				; $6684
			LD E,L				; $6685
			LD D,H				; $6686
			ADD HL,HL				; $6687
			ADD HL,HL				; $6688
			LD C,L				; $6689
			LD B,H				; $668A
			ADD HL,HL				; $668B
			ADD HL,BC				; $668C
			ADD HL,DE				; $668D
			LD ($66A8),HL				; $668E
			LD A,L				; $6691
			XOR H				; $6692
			LD E,A				; $6693
			LD D,$00				; $6694
			POP AF				; $6696
			LD HL,$0000				; $6697
			LD B,$08				; $669A
			ADD HL,HL				; $669C
			RLA				; $669D
			JR NC,$66A1				; $669E
			ADD HL,DE				; $66A0
			DJNZ $669C				; $66A1
			LD A,H				; $66A3
			POP BC				; $66A4
			POP DE				; $66A5
			POP HL				; $66A6
			RET				; $66A7

			NOP				; $66A8
			NOP				; $66A9

			LD C,$00				; $66AA
			LD HL,$5AFF				; $66AC
			LD DE,$5AFE				; $66AF
			LD (HL),C				; $66B2
			LD BC,$0300				; $66B3
			LDDR				; $66B6
			LD (HL),$00				; $66B8
			LD BC,$17FF				; $66BA
			LDDR				; $66BD
			RET				; $66BF

			LD C,$00				; $66C0
			LD HL,$5880				; $66C2
			LD (HL),C				; $66C5
			LD DE,$5881				; $66C6
			LD BC,$027F				; $66C9
			LDIR				; $66CC
			LD HL,$4080				; $66CE
			LD B,$A0				; $66D1
			PUSH BC				; $66D3
			PUSH HL				; $66D4
			LD E,L				; $66D5
			LD D,H				; $66D6
			INC DE				; $66D7
			LD (HL),$00				; $66D8
			LD BC,$001F				; $66DA
			LDIR				; $66DD
			POP HL				; $66DF
			CALL $674C				; $66E0
			POP BC				; $66E3
			DJNZ $66D3				; $66E4
			RET				; $66E6

			LD HL,$6300				; $66E7
			LD (HL),$00				; $66EA
			LD DE,$6301				; $66EC
			LD BC,$001F				; $66EF
			LDIR				; $66F2
			LD HL,$6400				; $66F4
			LD (HL),$00				; $66F7
			LD DE,$6401				; $66F9
			LD BC,$001F				; $66FC
			LDIR				; $66FF
			LD IX,$6320				; $6701
			LD IY,$6420				; $6705
			LD HL,$4080				; $6709
			LD B,$A0				; $670C
			LD (IX+$00),H				; $670E
			LD (IY+$00),L				; $6711
			CALL $674C				; $6714
			INC IX				; $6717
			INC IY				; $6719
			DJNZ $670E				; $671B
			RET				; $671D

			LD IX,$6300				; $671E
			LD IY,$6400				; $6722
			LD HL,$4000				; $6726
			LD B,$C0				; $6729
			LD (IX+$00),H				; $672B
			LD (IY+$00),L				; $672E
			CALL $674C				; $6731
			INC IX				; $6734
			INC IY				; $6736
			DJNZ $672B				; $6738
			RET				; $673A

			PUSH AF				; $673B
			LD L,D				; $673C
			LD H,$63				; $673D
			LD A,(HL)				; $673F
			INC H				; $6740
			LD L,(HL)				; $6741
			LD H,A				; $6742
			LD A,E				; $6743
			AND $7C				; $6744
			RRCA				; $6746
			RRCA				; $6747
			ADD A,L				; $6748
			LD L,A				; $6749
			POP AF				; $674A
			RET				; $674B

			INC H				; $674C
			LD A,H				; $674D
			AND $07				; $674E
			RET NZ				; $6750
			LD A,L				; $6751
			ADD A,$20				; $6752
			LD L,A				; $6754
			RET C				; $6755
			LD A,H				; $6756
			SUB $08				; $6757
			LD H,A				; $6759
			RET				; $675A

			LD A,H				; $675B
			DEC H				; $675C
			AND $07				; $675D
			RET NZ				; $675F
			LD A,L				; $6760
			SUB $20				; $6761
			LD L,A				; $6763
			RET C				; $6764
			LD A,H				; $6765
			ADD A,$08				; $6766
			LD H,A				; $6768
			RET				; $6769

			PUSH BC				; $676A
			PUSH DE				; $676B
			PUSH HL				; $676C
			LD HL,$0000				; $676D
			LD DE,$0000				; $6770
			LDIR				; $6773
			POP HL				; $6775
			POP DE				; $6776
			POP BC				; $6777
			RET				; $6778

			PUSH AF				; $6779
			PUSH DE				; $677A
			LD A,D				; $677B
			AND $F8				; $677C
			LD L,A				; $677E
			LD H,$00				; $677F
			ADD HL,HL				; $6781
			ADD HL,HL				; $6782
			LD A,E				; $6783
			AND $7C				; $6784
			RRCA				; $6786
			RRCA				; $6787
			LD E,A				; $6788
			LD D,$00				; $6789
			ADD HL,DE				; $678B
			POP DE				; $678C
			POP AF				; $678D
			RET				; $678E

			PUSH DE				; $678F
			CALL $6779				; $6790
			LD DE,$5F00				; $6793
			ADD HL,DE				; $6796
			POP DE				; $6797
			RET				; $6798

			PUSH DE				; $6799
			CALL $6779				; $679A
			LD DE,$5800				; $679D
			ADD HL,DE				; $67A0
			POP DE				; $67A1
			RET				; $67A2

			PUSH DE				; $67A3
			CALL $6779				; $67A4
			LD DE,$5B00				; $67A7
			ADD HL,DE				; $67AA
			POP DE				; $67AB
			RET				; $67AC

			LD HL,$5800				; $67AD
			LD DE,$5B00				; $67B0
			LD BC,$0300				; $67B3
			LDIR				; $67B6
			RET				; $67B8

			PUSH AF				; $67B9
			PUSH BC				; $67BA
			PUSH HL				; $67BB
			PUSH IX				; $67BC
			ADD A,A				; $67BE
			ADD A,A				; $67BF
			LD L,A				; $67C0
			LD H,$00				; $67C1
			LD BC,$67EE				; $67C3
			ADD HL,BC				; $67C6
			PUSH HL				; $67C7
			POP IX				; $67C8
			LD A,(IX+$02)				; $67CA
			LD ($6805),A				; $67CD
			ADD A,(IX+$00)				; $67D0
			NEG				; $67D3
			INC A				; $67D5
			LD ($6807),A				; $67D6
			LD A,(IX+$03)				; $67D9
			LD ($680E),A				; $67DC
			ADD A,(IX+$01)				; $67DF
			NEG				; $67E2
			INC A				; $67E4
			LD ($6810),A				; $67E5
			POP IX				; $67E8
			POP HL				; $67EA
			POP BC				; $67EB
			POP AF				; $67EC
			RET				; $67ED

			defb $04,$08,$04,$08,$08                            ; $67EE .....
			defb $10,$08,$10,$08,$10,$04,$08,$08				; $67F3 ........
			defb $10,$01,$01,$00,$00,$08,$10                    ; $67FB .......

			LD A,E				; $6802
			SUB C				; $6803
			SUB $08				; $6804
			CP $F0				; $6806
			LD A,$00				; $6808
			RET C				; $680A
			LD A,D				; $680B
			SUB B				; $680C
			SUB $10				; $680D
			CP $E0				; $680F
			LD A,$00				; $6811
			RET C				; $6813
			INC A				; $6814
			RET				; $6815

			LD D,$01				; $6816
			LD HL,$68A6				; $6818
			LD C,$FE				; $681B
			INC HL				; $681D
			LD B,$DF				; $681E
			IN A,(C)				; $6820
			AND $02				; $6822
			JR NZ,$6827				; $6824
			LD (HL),D				; $6826
			INC HL				; $6827
			LD B,$DF				; $6828
			IN A,(C)				; $682A
			AND $01				; $682C
			JR NZ,$6831				; $682E
			LD (HL),D				; $6830
			INC HL				; $6831
			LD B,$FB				; $6832
			IN A,(C)				; $6834
			AND $01				; $6836
			JR NZ,$683B				; $6838
			LD (HL),D				; $683A
			INC HL				; $683B
			LD B,$7F				; $683C
			IN A,(C)				; $683E
			AND $01				; $6840
			RET NZ				; $6842
			LD (HL),D				; $6843
			RET				; $6844

			LD BC,$EFFE				; $6845
			IN A,(C)				; $6848
			CPL				; $684A
			AND $1F				; $684B
			LD D,A				; $684D
			CALL $688A				; $684E
			LD ($68AA),A				; $6851
			CALL $688A				; $6854
			LD ($68A9),A				; $6857
			CALL $688A				; $685A
			CALL $688A				; $685D
			LD ($68A8),A				; $6860
			CALL $688A				; $6863
			LD ($68A7),A				; $6866
			RET				; $6869

			LD C,$1F				; $686A
			IN D,(C)				; $686C
			CALL $688A				; $686E
			LD ($68A8),A				; $6871
			CALL $688A				; $6874
			LD ($68A7),A				; $6877
			CALL $688A				; $687A
			CALL $688A				; $687D
			LD ($68A9),A				; $6880
			CALL $688A				; $6883
			LD ($68AA),A				; $6886
			RET				; $6889

			XOR A				; $688A
			SRL D				; $688B
			RLA				; $688D
			RET				; $688E

			LD HL,$0000				; $688F
			LD ($68A7),HL				; $6892
			LD ($68A9),HL				; $6895
			LD A,($68AB)				; $6898
			OR A				; $689B
			JP Z,$6816				; $689C
			CP $01				; $689F
			JP Z,$6845				; $68A1
			JP $686A				; $68A4

			NOP				; $68A7
			NOP				; $68A8
			NOP				; $68A9
			NOP				; $68AA
			NOP				; $68AB

			LD A,($8F94)				; $68AC
			OR A				; $68AF
			RET NZ				; $68B0
			LD ($696F),A				; $68B1
			LD DE,($6969)				; $68B4
			LD ($6A08),DE				; $68B8
			LD HL,($68A7)				; $68BC
			LD A,H				; $68BF
			XOR L				; $68C0
			JR Z,$690E				; $68C1
			BIT 0,L				; $68C3
			JR NZ,$68EC				; $68C5
			LD A,($696B)				; $68C7
			CP $FF				; $68CA
			LD A,$01				; $68CC
			LD ($696B),A				; $68CE
			JR NZ,$68D9				; $68D1
			LD ($696F),A				; $68D3
			JP $690E				; $68D6

			LD A,E				; $68D9
			CP $78				; $68DA
			CALL Z,$71B4				; $68DC
			CALL $6972				; $68DF
			JR NZ,$690E				; $68E2
			INC E				; $68E4
			LD A,$01				; $68E5
			LD ($696F),A				; $68E7
			JR $690E				; $68EA
			LD A,($696B)				; $68EC
			CP $01				; $68EF
			LD A,$FF				; $68F1
			LD ($696B),A				; $68F3
			JR NZ,$68FE				; $68F6
			LD ($696F),A				; $68F8
			JP $690E				; $68FB

			LD A,E				; $68FE
			OR A				; $68FF
			CALL Z,$71B4				; $6900
			CALL $699C				; $6903
			JR NZ,$690E				; $6906
			DEC E				; $6908
			LD A,$01				; $6909
			LD ($696F),A				; $690B
			LD A,($68A9)				; $690E
			OR A				; $6911
			JR Z,$692D				; $6912
			LD A,$FE				; $6914
			LD ($696C),A				; $6916
			LD A,D				; $6919
			CP $20				; $691A
			CALL Z,$71B4				; $691C
			CALL $69E6				; $691F
			JR NZ,$6944				; $6922
			DEC D				; $6924
			DEC D				; $6925
			LD A,$01				; $6926
			LD ($696F),A				; $6928
			JR $6944				; $692B
			LD A,$02				; $692D
			LD ($696C),A				; $692F
			LD A,D				; $6932
			CP $B0				; $6933
			CALL Z,$71B4				; $6935
			CALL $69C4				; $6938
			JR NZ,$6944				; $693B
			INC D				; $693D
			INC D				; $693E
			LD A,$01				; $693F
			LD ($696F),A				; $6941
			LD A,($696F)				; $6944
			OR A				; $6947
			JP Z,$6A46				; $6948
			LD BC,($6969)				; $694B
			LD ($6969),DE				; $694F
			LD A,($696B)				; $6953
			LD L,$01				; $6956
			INC A				; $6958
			JR NZ,$695C				; $6959
			INC L				; $695B
			LD A,L				; $695C
			LD HL,($696D)				; $695D
			CALL $A1FD				; $6960
			LD ($696D),HL				; $6963
			JP $6A0A				; $6966

			defb $40,$58                                        ; $6969 @X
			defb $01,$02,$F9,$C6,$01,$00,$00                    ; $696B .......

			PUSH BC				; $6972
			PUSH DE				; $6973
			PUSH HL				; $6974
			LD A,E				; $6975
			AND $03				; $6976
			LD A,$00				; $6978
			JR NZ,$6997				; $697A
			LD A,E				; $697C
			CP $78				; $697D
			LD A,$00				; $697F
			JR NC,$6997				; $6981
			CALL $678F				; $6983
			INC L				; $6986
			INC L				; $6987
			LD BC,$0020				; $6988
			LD A,(HL)				; $698B
			ADD HL,BC				; $698C
			OR (HL)				; $698D
			LD E,A				; $698E
			LD A,D				; $698F
			AND $07				; $6990
			LD A,E				; $6992
			JR Z,$6997				; $6993
			ADD HL,BC				; $6995
			OR (HL)				; $6996
			OR A				; $6997
			POP HL				; $6998
			POP DE				; $6999
			POP BC				; $699A
			RET				; $699B

			PUSH BC				; $699C
			PUSH DE				; $699D
			PUSH HL				; $699E
			LD A,E				; $699F
			AND $03				; $69A0
			LD A,$00				; $69A2
			JR NZ,$69BF				; $69A4
			LD A,E				; $69A6
			OR A				; $69A7
			LD A,$00				; $69A8
			JR Z,$69BF				; $69AA
			CALL $678F				; $69AC
			DEC L				; $69AF
			LD BC,$0020				; $69B0
			LD A,(HL)				; $69B3
			ADD HL,BC				; $69B4
			OR (HL)				; $69B5
			LD E,A				; $69B6
			LD A,D				; $69B7
			AND $07				; $69B8
			LD A,E				; $69BA
			JR Z,$69BF				; $69BB
			ADD HL,BC				; $69BD
			OR (HL)				; $69BE
			OR A				; $69BF
			POP HL				; $69C0
			POP DE				; $69C1
			POP BC				; $69C2
			RET				; $69C3

			PUSH BC				; $69C4
			PUSH DE				; $69C5
			PUSH HL				; $69C6
			LD A,D				; $69C7
			AND $07				; $69C8
			LD A,$00				; $69CA
			JR NZ,$69E1				; $69CC
			CALL $678F				; $69CE
			LD BC,$0040				; $69D1
			ADD HL,BC				; $69D4
			LD A,(HL)				; $69D5
			INC L				; $69D6
			OR (HL)				; $69D7
			LD D,A				; $69D8
			LD A,E				; $69D9
			AND $03				; $69DA
			LD A,D				; $69DC
			JR Z,$69E1				; $69DD
			INC L				; $69DF
			OR (HL)				; $69E0
			OR A				; $69E1
			POP HL				; $69E2
			POP DE				; $69E3
			POP BC				; $69E4
			RET				; $69E5
			PUSH BC				; $69E6
			PUSH DE				; $69E7
			PUSH HL				; $69E8
			LD A,D				; $69E9
			AND $07				; $69EA
			LD A,$00				; $69EC
			JR NZ,$6A03				; $69EE
			CALL $678F				; $69F0
			LD BC,$FFE0				; $69F3
			ADD HL,BC				; $69F6
			LD A,(HL)				; $69F7
			INC L				; $69F8
			OR (HL)				; $69F9
			LD D,A				; $69FA
			LD A,E				; $69FB
			AND $03				; $69FC
			LD A,D				; $69FE
			JR Z,$6A03				; $69FF
			INC L				; $6A01
			OR (HL)				; $6A02
			OR A				; $6A03
			POP HL				; $6A04
			POP DE				; $6A05
			POP BC				; $6A06
			RET				; $6A07

			NOP				; $6A08
			NOP				; $6A09

			LD A,($6AEE)				; $6A0A
			OR A				; $6A0D
			JP Z,$6A46				; $6A0E
			LD HL,($6AEF)				; $6A11
			LD BC,($6AF1)				; $6A14
			LD DE,($6969)				; $6A18
			LD A,($696B)				; $6A1C
			CP $FF				; $6A1F
			JR Z,$6A36				; $6A21
			LD A,E				; $6A23
			SUB $08				; $6A24
			LD E,A				; $6A26
			LD ($6AF1),DE				; $6A27
			LD A,$07				; $6A2B
			CALL $A1FD				; $6A2D
			LD ($6AEF),HL				; $6A30
			JP $6A46				; $6A33

			LD A,E				; $6A36
			ADD A,$08				; $6A37
			LD E,A				; $6A39
			LD ($6AF1),DE				; $6A3A
			LD A,$08				; $6A3E
			CALL $A1FD				; $6A40
			LD ($6AEF),HL				; $6A43
			LD A,($6AF3)				; $6A46
			OR A				; $6A49
			JR Z,$6ABA				; $6A4A
			LD DE,($6AF6)				; $6A4C
			LD A,($8F94)				; $6A50
			OR A				; $6A53
			JP NZ,$6A87				; $6A54
			LD A,($696B)				; $6A57
			CP $FF				; $6A5A
			LD A,($6AF8)				; $6A5C
			JR Z,$6A6B				; $6A5F
			CP $17				; $6A61
			JR C,$6A67				; $6A63
			LD A,$FF				; $6A65
			INC A				; $6A67
			JP $6A71				; $6A68

			OR A				; $6A6B
			JR NZ,$6A70				; $6A6C
			LD A,$18				; $6A6E
			DEC A				; $6A70
			LD ($6AF8),A				; $6A71
			ADD A,A				; $6A74
			LD L,A				; $6A75
			LD H,$00				; $6A76
			LD BC,$6AF9				; $6A78
			ADD HL,BC				; $6A7B
			LD DE,($6969)				; $6A7C
			LD A,(HL)				; $6A80
			ADD A,E				; $6A81
			LD E,A				; $6A82
			INC HL				; $6A83
			LD A,(HL)				; $6A84
			ADD A,D				; $6A85
			LD D,A				; $6A86
			LD BC,($6AF6)				; $6A87
			LD ($6AF6),DE				; $6A8B
			LD HL,($6AF4)				; $6A8F
			LD A,$11				; $6A92
			CALL $A1FD				; $6A94
			LD ($6AF4),HL				; $6A97
			LD A,$7B				; $6A9A
			CP E				; $6A9C
			CALL NC,$8ABF				; $6A9D
			LD A,$01				; $6AA0
			CALL $67B9				; $6AA2
			CALL $9A75				; $6AA5
			JR Z,$6AB1				; $6AA8
			PUSH DE				; $6AAA
			LD E,$21				; $6AAB
			CALL $EF42				; $6AAD
			POP DE				; $6AB0
			INC E				; $6AB1
			INC E				; $6AB2
			INC D				; $6AB3
			INC D				; $6AB4
			INC D				; $6AB5
			INC D				; $6AB6
			CALL $6C9A				; $6AB7
			LD C,$47				; $6ABA
			LD A,($7E94)				; $6ABC
			OR A				; $6ABF
			JR Z,$6ACE				; $6AC0
			DEC A				; $6AC2
			LD ($7E94),A				; $6AC3
			LD A,($65EA)				; $6AC6
			AND $07				; $6AC9
			OR $40				; $6ACB
			LD C,A				; $6ACD
			LD DE,($6969)				; $6ACE
			CALL $A446				; $6AD2
			LD A,($6AEE)				; $6AD5
			OR A				; $6AD8
			JR Z,$6AE2				; $6AD9
			LD DE,($6AF1)				; $6ADB
			CALL $A446				; $6ADF
			LD A,($6AF3)				; $6AE2
			OR A				; $6AE5
			RET Z				; $6AE6
			LD DE,($6AF6)				; $6AE7
			JP $A446				; $6AEB

			defb $00,$00,$00,$00,$00                            ; $6AEE ....
			defb $00,$00,$00,$00,$00,$00,$00,$E0				; $6AF3 ........
			defb $04,$E2,$08,$E6,$0B,$EB,$0D,$F1				; $6AFB ........
			defb $0F,$F8,$0F,$00,$0F,$08,$0D,$0F				; $6B03 ........
			defb $0B,$15,$08,$1A,$04,$1E,$00,$20				; $6B0B ....... 
			defb $FC,$1E,$F8,$1A,$F5,$15,$F3,$0F				; $6B13 ........
			defb $F1,$08,$F1,$00,$F1,$F8,$F3,$F1				; $6B1B ........
			defb $F5,$EB,$F8,$E6,$FC,$E2,$7E,$23				; $6B23 ......~#

			CP $61				; $6B2B
			JP NC,$6B37				; $6B2D
			CALL $6C4A				; $6B30
			INC E				; $6B33
			JP $6B29				; $6B34

			CP $90				; $6B37
			JP NC,$6B47				; $6B39
			SUB $78				; $6B3C
			ADD A,D				; $6B3E
			LD D,A				; $6B3F
			LD A,(HL)				; $6B40
			ADD A,E				; $6B41
			LD E,A				; $6B42
			INC HL				; $6B43
			JP $6B29				; $6B44

			CP $CF				; $6B47
			JP NC,$6B54				; $6B49
			INC D				; $6B4C
			SUB $AF				; $6B4D
			ADD A,E				; $6B4F
			LD E,A				; $6B50
			JP $6B29				; $6B51

			CP $DF				; $6B54
			JP NC,$6B6D				; $6B56
			SUB $CF				; $6B59
			CP $08				; $6B5B
			JP C,$6B64				; $6B5D
			SUB $08				; $6B60
			OR $40				; $6B62
			LD B,A				; $6B64
			LD A,C				; $6B65
			AND $38				; $6B66
			OR B				; $6B68
			LD C,A				; $6B69
			JP $6B29				; $6B6A
			CP $DF				; $6B6D
			JP NZ,$6B79				; $6B6F
			LD D,(HL)				; $6B72
			INC HL				; $6B73
			LD E,(HL)				; $6B74
			INC HL				; $6B75
			JP $6B29				; $6B76
			CP $E0				; $6B79
			JP NZ,$6B83				; $6B7B
			LD C,(HL)				; $6B7E
			INC HL				; $6B7F
			JP $6B29				; $6B80

			CP $E1				; $6B83
			JP NZ,$6B8F				; $6B85
			LD B,(HL)				; $6B88
			INC HL				; $6B89
			PUSH HL				; $6B8A
			PUSH BC				; $6B8B
			JP $6B29				; $6B8C

			CP $E2				; $6B8F
			JP NZ,$6B9F				; $6B91
			POP BC				; $6B94
			DJNZ $6B9B				; $6B95
			POP AF				; $6B97
			JP $6B29				; $6B98

			POP HL				; $6B9B
			JP $6B8A				; $6B9C

			CP $E3				; $6B9F
			JP NZ,$6BB5				; $6BA1
			LD A,(HL)				; $6BA4
			INC HL				; $6BA5
			PUSH HL				; $6BA6
			LD H,(HL)				; $6BA7
			LD L,A				; $6BA8
			PUSH BC				; $6BA9
			PUSH DE				; $6BAA
			CALL $6B29				; $6BAB
			POP DE				; $6BAE
			POP BC				; $6BAF
			POP HL				; $6BB0
			INC HL				; $6BB1
			JP $6B29				; $6BB2

			CP $E4				; $6BB5
			JP NZ,$6BC7				; $6BB7
			LD B,(HL)				; $6BBA
			INC HL				; $6BBB
			LD A,(HL)				; $6BBC
			CALL $6C4A				; $6BBD
			INC E				; $6BC0
			DJNZ $6BBD				; $6BC1
			INC HL				; $6BC3
			JP $6B29				; $6BC4

			CP $E5				; $6BC7
			JP NZ,$6BD9				; $6BC9
			LD B,(HL)				; $6BCC
			INC HL				; $6BCD
			LD A,(HL)				; $6BCE
			CALL $6C4A				; $6BCF
			INC D				; $6BD2
			DJNZ $6BCF				; $6BD3
			INC HL				; $6BD5
			JP $6B29				; $6BD6

			CP $E6				; $6BD9
			JR NZ,$6BEA				; $6BDB
			LD A,(HL)				; $6BDD
			LD ($6C55),A				; $6BDE
			INC HL				; $6BE1
			LD A,(HL)				; $6BE2
			LD ($6C56),A				; $6BE3
			INC HL				; $6BE6
			JP $6B29				; $6BE7

			CP $E7				; $6BEA
			JR NZ,$6C07				; $6BEC
			PUSH HL				; $6BEE
			LD HL,($6C55)				; $6BEF
			PUSH HL				; $6BF2
			LD HL,$C2F1				; $6BF3
			LD ($6C55),HL				; $6BF6
			LD A,$20				; $6BF9
			CALL $6C4A				; $6BFB
			INC E				; $6BFE
			POP HL				; $6BFF
			LD ($6C55),HL				; $6C00
			POP HL				; $6C03
			JP $6B29				; $6C04
			CP $E8				; $6C07
			JR NZ,$6C13				; $6C09
			LD A,(HL)				; $6C0B
			LD ($6C82),A				; $6C0C
			INC HL				; $6C0F
			JP $6B29				; $6C10

			CP $E9				; $6C13
			JR NZ,$6C1A				; $6C15
			JP $6B29				; $6C17

			CP $EA				; $6C1A
			JR NZ,$6C21				; $6C1C
			JP $6B29				; $6C1E

			CP $EB				; $6C21
			RET NZ				; $6C23
			PUSH BC				; $6C24
			PUSH HL				; $6C25
			LD L,(HL)				; $6C26
			LD H,$00				; $6C27
			ADD HL,HL				; $6C29
			LD BC,$6C44				; $6C2A
			ADD HL,BC				; $6C2D
			LD A,(HL)				; $6C2E
			INC HL				; $6C2F
			LD H,(HL)				; $6C30
			LD L,A				; $6C31
			LD ($6B31),HL				; $6C32
			LD ($6BBE),HL				; $6C35
			LD ($6BD0),HL				; $6C38
			LD ($6BFC),HL				; $6C3B
			POP HL				; $6C3E
			POP BC				; $6C3F
			INC HL				; $6C40
			JP $6B29				; $6C41

			defb $4A,$6C,$87,$6C,$87,$6C                        ; $6C44 Jl.l.l

			PUSH AF				; $6C4A
			PUSH DE				; $6C4B
			PUSH HL				; $6C4C
			PUSH BC				; $6C4D
			LD L,A				; $6C4E
			LD H,$00				; $6C4F
			ADD HL,HL				; $6C51
			ADD HL,HL				; $6C52
			ADD HL,HL				; $6C53
			LD BC,$0000				; $6C54
			ADD HL,BC				; $6C57
			PUSH HL				; $6C58
			LD A,D				; $6C59
			AND $F8				; $6C5A
			OR $40				; $6C5C
			LD B,A				; $6C5E
			LD A,D				; $6C5F
			LD H,B				; $6C60
			AND $07				; $6C61
			RRCA				; $6C63
			RRCA				; $6C64
			RRCA				; $6C65
			ADD A,E				; $6C66
			LD L,A				; $6C67
			POP DE				; $6C68
			LD B,$08				; $6C69
			LD A,(DE)				; $6C6B
			INC DE				; $6C6C
			LD (HL),A				; $6C6D
			INC H				; $6C6E
			DJNZ $6C6B				; $6C6F
			DEC H				; $6C71
			POP BC				; $6C72
			LD A,H				; $6C73
			RRCA				; $6C74
			RRCA				; $6C75
			RRCA				; $6C76
			AND $03				; $6C77
			OR $58				; $6C79
			LD H,A				; $6C7B
			LD (HL),C				; $6C7C
			LD DE,$0700				; $6C7D
			ADD HL,DE				; $6C80
			LD (HL),$00				; $6C81
			POP HL				; $6C83
			POP DE				; $6C84
			POP AF				; $6C85
			RET				; $6C86

			RET				; $6C87

			LD A,$06				; $6C88
			CALL $6679				; $6C8A
			SUB $03				; $6C8D
			LD C,A				; $6C8F
			LD A,$0C				; $6C90
			CALL $6679				; $6C92
			INC A				; $6C95
			NEG				; $6C96
			LD B,A				; $6C98
			RET				; $6C99

			PUSH AF				; $6C9A
			PUSH BC				; $6C9B
			PUSH DE				; $6C9C
			PUSH HL				; $6C9D
			PUSH IX				; $6C9E
			LD B,D				; $6CA0
			LD C,E				; $6CA1
			DEC C				; $6CA2
			DEC C				; $6CA3
			DEC B				; $6CA4
			DEC B				; $6CA5
			DEC B				; $6CA6
			DEC B				; $6CA7
			LD IX,$6CFC				; $6CA8
			LD A,(IX+$00)				; $6CAC
			CP $FF				; $6CAF
			JR Z,$6CEE				; $6CB1
			LD A,(IX+$04)				; $6CB3
			OR A				; $6CB6
			JR Z,$6CF5				; $6CB7
			LD A,(IX+$02)				; $6CB9
			LD ($67FE),A				; $6CBC
			LD A,(IX+$03)				; $6CBF
			LD ($67FF),A				; $6CC2
			LD A,$04				; $6CC5
			CALL $67B9				; $6CC7
			LD E,(IX+$00)				; $6CCA
			LD D,(IX+$01)				; $6CCD
			CALL $6802				; $6CD0
			OR A				; $6CD3
			JR Z,$6CF5				; $6CD4
			LD (IX+$04),$00				; $6CD6
			LD BC,($67FE)				; $6CDA
			LD A,$F8				; $6CDE
			ADD A,C				; $6CE0
			LD C,A				; $6CE1
			LD A,$F0				; $6CE2
			ADD A,B				; $6CE4
			LD B,A				; $6CE5
			CALL $6D4D				; $6CE6
			LD E,$1E				; $6CE9
			CALL $EF42				; $6CEB
			POP IX				; $6CEE
			POP HL				; $6CF0
			POP DE				; $6CF1
			POP BC				; $6CF2
			POP AF				; $6CF3
			RET				; $6CF4

			defb $11,$08,$00,$DD,$19,$18                        ; $6CF5 ......
			defb $B0,$4C,$44,$09,$48,$2C,$28,$48				; $6CFB .LD.H,(H
			defb $4C,$29,$0D,$0A,$24,$33,$3A,$09				; $6D03 L)..$3:.
			defb $4F,$52,$09,$30,$0D,$0A,$09,$4C				; $6D0B OR.0...L
			defb $44,$09,$4C,$2C,$41,$0D,$0A,$09				; $6D13 D.L,A...
			defb $49,$4E,$43,$09,$43,$0D,$0A,$0D				; $6D1B INC.C...
			defb $0A,$09,$4C,$44,$09,$41,$2C,$28				; $6D23 ..LD.A,(
			defb $44,$45,$29,$0D,$0A,$09,$58,$4F				; $6D2B DE)...XO
			defb $52,$09,$28,$48,$4C,$29,$0D,$0A				; $6D33 R.(HL)..
			defb $24,$39,$3A,$09,$4C,$44,$09,$28				; $6D3B $9:.LD.(
			defb $48,$4C,$29,$2C,$41,$0D,$0A,$09				; $6D43 HL),A...
			defb $49,$FF                                        ; $6D4B I.

			LD E,(IX+$00)				; $6D4D
			LD D,(IX+$01)				; $6D50
			PUSH DE				; $6D53
			LD A,(IX+$05)				; $6D54
			CALL $8A3E				; $6D57
			LD E,(IX+$06)				; $6D5A
			LD D,(IX+$07)				; $6D5D
			CALL $78D4				; $6D60
			CALL $788E				; $6D63
			POP DE				; $6D66
			LD A,(IX+$02)				; $6D67
			AND $F8				; $6D6A
			RRCA				; $6D6C
			RRCA				; $6D6D
			RRCA				; $6D6E
			LD B,A				; $6D6F
			LD A,(IX+$03)				; $6D70
			AND $F0				; $6D73
			RRCA				; $6D75
			RRCA				; $6D76
			RRCA				; $6D77
			RRCA				; $6D78
			LD C,A				; $6D79
			PUSH DE				; $6D7A
			PUSH BC				; $6D7B
			CALL $678F				; $6D7C
			LD A,(HL)				; $6D7F
			OR A				; $6D80
			CALL NZ,$6D96				; $6D81
			INC HL				; $6D84
			INC HL				; $6D85
			LD A,E				; $6D86
			ADD A,$08				; $6D87
			LD E,A				; $6D89
			DJNZ $6D7F				; $6D8A
			POP BC				; $6D8C
			POP DE				; $6D8D
			LD A,D				; $6D8E
			ADD A,$10				; $6D8F
			LD D,A				; $6D91
			DEC C				; $6D92
			JR NZ,$6D7A				; $6D93
			RET				; $6D95

			PUSH BC				; $6D96
			PUSH DE				; $6D97
			PUSH HL				; $6D98
			LD HL,$C6F9				; $6D99
			CALL $A4DB				; $6D9C
			CALL $88EA				; $6D9F
			LD L,A				; $6DA2
			LD H,$00				; $6DA3
			ADD HL,HL				; $6DA5
			ADD HL,HL				; $6DA6
			LD BC,$E9B9				; $6DA7
			ADD HL,BC				; $6DAA
			CALL $6DE0				; $6DAB
			LD C,(HL)				; $6DAE
			CALL $6E8B				; $6DAF
			CALL $6E8B				; $6DB2
			INC HL				; $6DB5
			LD A,E				; $6DB6
			ADD A,$04				; $6DB7
			LD E,A				; $6DB9
			LD C,(HL)				; $6DBA
			CALL $6E8B				; $6DBB
			CALL $6E8B				; $6DBE
			INC HL				; $6DC1
			SUB $04				; $6DC2
			LD E,A				; $6DC4
			LD A,D				; $6DC5
			ADD A,$08				; $6DC6
			LD D,A				; $6DC8
			LD C,(HL)				; $6DC9
			CALL $6E8B				; $6DCA
			CALL $6E8B				; $6DCD
			INC HL				; $6DD0
			LD A,E				; $6DD1
			ADD A,$04				; $6DD2
			LD E,A				; $6DD4
			LD C,(HL)				; $6DD5
			CALL $6E8B				; $6DD6
			CALL $6E8B				; $6DD9
			POP HL				; $6DDC
			POP DE				; $6DDD
			POP BC				; $6DDE
			RET				; $6DDF

			PUSH AF				; $6DE0
			PUSH BC				; $6DE1
			PUSH DE				; $6DE2
			PUSH HL				; $6DE3
			CALL $67A3				; $6DE4
			XOR A				; $6DE7
			PUSH HL				; $6DE8
			LD (HL),A				; $6DE9
			INC L				; $6DEA
			LD (HL),A				; $6DEB
			LD DE,$001F				; $6DEC
			ADD HL,DE				; $6DEF
			LD (HL),A				; $6DF0
			INC L				; $6DF1
			LD (HL),A				; $6DF2
			POP HL				; $6DF3
			LD DE,$0400				; $6DF4
			ADD HL,DE				; $6DF7
			LD (HL),A				; $6DF8
			INC L				; $6DF9
			LD (HL),A				; $6DFA
			LD DE,$001F				; $6DFB
			ADD HL,DE				; $6DFE
			LD (HL),A				; $6DFF
			INC L				; $6E00
			LD (HL),A				; $6E01
			POP HL				; $6E02
			POP DE				; $6E03
			POP BC				; $6E04
			POP AF				; $6E05
			RET				; $6E06

			PUSH AF				; $6E07
			PUSH BC				; $6E08
			PUSH HL				; $6E09
			LD HL,$6E52				; $6E0A
			LD C,A				; $6E0D
			LD A,(HL)				; $6E0E
			CP $FF				; $6E0F
			LD A,C				; $6E11
			JR Z,$6E1D				; $6E12
			CP (HL)				; $6E14
			JR Z,$6E21				; $6E15
			LD BC,$0008				; $6E17
			ADD HL,BC				; $6E1A
			JR $6E0D				; $6E1B
			POP HL				; $6E1D
			POP BC				; $6E1E
			POP AF				; $6E1F
			RET				; $6E20

			LD (IX+$04),A				; $6E21
			INC HL				; $6E24
			LD A,(HL)				; $6E25
			ADD A,E				; $6E26
			LD (IX+$00),A				; $6E27
			INC HL				; $6E2A
			LD A,(HL)				; $6E2B
			ADD A,D				; $6E2C
			LD (IX+$01),A				; $6E2D
			INC HL				; $6E30
			LD A,(HL)				; $6E31
			LD (IX+$02),A				; $6E32
			INC HL				; $6E35
			LD A,(HL)				; $6E36
			LD (IX+$03),A				; $6E37
			INC HL				; $6E3A
			LD A,(HL)				; $6E3B
			LD (IX+$05),A				; $6E3C
			INC HL				; $6E3F
			LD A,(HL)				; $6E40
			LD (IX+$06),A				; $6E41
			INC HL				; $6E44
			LD A,(HL)				; $6E45
			LD (IX+$07),A				; $6E46
			LD BC,$0008				; $6E49
			ADD IX,BC				; $6E4C
			POP HL				; $6E4E
			POP BC				; $6E4F
			POP AF				; $6E50
			RET				; $6E51

			defb $27                                            ; $6E52 '
			defb $00,$00,$10,$20,$3A,$16,$79,$23				; $6E53 ... :.y#
			defb $00,$00,$10,$20,$38,$0A,$79,$2B				; $6E5B ... 8.y+
			defb $00,$00,$10,$20,$39,$10,$79,$30				; $6E63 ... 9.y0
			defb $00,$00,$10,$20,$3A,$16,$79,$46				; $6E6B ... :.yF
			defb $00,$00,$08,$10,$39,$10,$79,$47				; $6E73 ....9.yG
			defb $00,$00,$08,$10,$39,$10,$79,$A0				; $6E7B ....9.y.
			defb $00,$00,$10,$30,$3A,$16,$79,$FF				; $6E83 ...0:.y.

			PUSH AF				; $6E8B
			PUSH DE				; $6E8C
			PUSH HL				; $6E8D
			PUSH BC				; $6E8E
			LD BC,$0006				; $6E8F
			LD HL,$6EC4				; $6E92
			LD A,(HL)				; $6E95
			OR A				; $6E96
			JR Z,$6EA0				; $6E97
			CP $FF				; $6E99
			JR Z,$6EBF				; $6E9B
			ADD HL,BC				; $6E9D
			JR $6E95				; $6E9E
			LD A,$01				; $6EA0
			CALL $6679				; $6EA2
			ADD A,$02				; $6EA5
			LD (HL),A				; $6EA7
			EX AF,AF'				; $6EA8
			INC HL				; $6EA9
			LD (HL),E				; $6EAA
			INC HL				; $6EAB
			LD (HL),D				; $6EAC
			INC HL				; $6EAD
			CALL $6C88				; $6EAE
			LD (HL),C				; $6EB1
			INC HL				; $6EB2
			LD (HL),B				; $6EB3
			EX AF,AF'				; $6EB4
			CALL $80BD				; $6EB5
			POP BC				; $6EB8
			PUSH BC				; $6EB9
			INC HL				; $6EBA
			LD A,C				; $6EBB
			AND $47				; $6EBC
			LD (HL),A				; $6EBE
			POP BC				; $6EBF
			POP HL				; $6EC0
			POP DE				; $6EC1
			POP AF				; $6EC2
			RET				; $6EC3

			defb $0A,$09,$45,$58,$09,$44,$45                    ; $6EC4 ..EX.DE
			defb $2C,$48,$4C,$0D,$0A,$09,$50,$4F				; $6ECB ,HL...PO
			defb $50,$09,$48,$4C,$0D,$0A,$09,$4C				; $6ED3 P.HL...L
			defb $44,$09,$41,$2C,$28,$44,$45,$29				; $6EDB D.A,(DE)
			defb $0D,$0A,$09,$4F,$52,$09,$41,$0D				; $6EE3 ...OR.A.
			defb $0A,$09,$4A,$52,$09,$4E,$5A,$2C				; $6EEB ..JR.NZ,
			defb $24,$33,$0D,$0A,$09,$4C,$44,$09				; $6EF3 $3...LD.
			defb $28,$48,$4C,$29,$2C,$43,$0D,$0A				; $6EFB (HL),C..
			defb $24,$33,$3A,$09,$49,$4E,$43,$09				; $6F03 $3:.INC.
			defb $48,$4C,$0D,$0A,$09,$49,$4E,$43				; $6F0B HL...INC
			defb $09,$44,$45,$0D,$0A,$09,$4C,$44				; $6F13 .DE...LD
			defb $09,$41,$2C,$28,$44,$45,$29,$0D				; $6F1B .A,(DE).
			defb $0A,$09,$4F,$52,$09,$41,$0D,$0A				; $6F23 ..OR.A..
			defb $09,$4A,$52,$09,$4E,$5A,$2C,$24				; $6F2B .JR.NZ,$
			defb $34,$0D,$0A,$09,$4C,$44,$09,$28				; $6F33 4...LD.(
			defb $48,$4C,$29,$2C,$43,$0D,$0A,$24				; $6F3B HL),C..$
			defb $34,$3A,$09,$49,$4E,$43,$09,$48				; $6F43 4:.INC.H
			defb $4C,$0D,$0A,$09,$49,$4E,$43,$09				; $6F4B L...INC.
			defb $44,$45,$0D,$0A,$09,$4C,$44,$09				; $6F53 DE...LD.
			defb $41,$2C,$28,$44,$45,$29,$0D,$0A				; $6F5B A,(DE)..
			defb $09,$4F,$52,$09,$41,$0D,$0A,$09				; $6F63 .OR.A...
			defb $4A,$52,$09,$4E,$5A,$2C,$24,$35				; $6F6B JR.NZ,$5
			defb $0D,$0A,$09,$4C,$44,$09,$28,$48				; $6F73 ...LD.(H
			defb $4C,$29,$2C,$43,$0D,$0A,$24,$35				; $6F7B L),C..$5
			defb $3A,$09,$4C,$44,$09,$44,$45,$2C				; $6F83 :.LD.DE,
			defb $33,$30,$0D,$0A,$09,$41,$44,$44				; $6F8B 30...ADD
			defb $09,$48,$4C,$2C,$44,$45,$0D,$0A				; $6F93 .HL,DE..
			defb $09,$44,$4A,$4E,$5A,$09,$24,$32				; $6F9B .DJNZ.$2
			defb $0D,$0A,$09,$50,$4F,$50,$09,$48				; $6FA3 ...POP.H
			defb $4C,$0D,$0A,$09,$50,$4F,$50,$09				; $6FAB L...POP.
			defb $44,$45,$0D,$0A,$09,$50,$4F,$50				; $6FB3 DE...POP
			defb $09,$42,$43,$0D,$0A,$09,$50,$4F				; $6FBB .BC...PO
			defb $50,$09,$41,$46,$0D,$0A,$09,$52				; $6FC3 P.AF...R
			defb $45,$54,$0D,$0A,$0D,$0A,$41,$54				; $6FCB ET....AT
			defb $52,$32,$32,$32,$3A,$09,$50,$55				; $6FD3 R222:.PU
			defb $53,$48,$09,$41,$46,$09,$09,$3B				; $6FDB SH.AF..;
			defb $53,$41,$4D,$45,$20,$41,$53,$20				; $6FE3 SAME AS 
			defb $41,$42,$4F,$56,$45,$20,$42,$55				; $6FEB ABOVE BU
			defb $54,$20,$44,$45,$41,$4C,$53,$20				; $6FF3 T DEALS 
			defb $57,$49,$54,$48,$20,$54,$57,$4F				; $6FFB WITH TWO
			defb $20,$43,$4F,$4C,$4F,$55,$52,$53				; $7003  COLOURS
			defb $20,$28,$42,$2B,$43,$29,$0D,$0A				; $700B  (B+C)..
			defb $09,$50,$55,$53,$48,$09,$42,$43				; $7013 .PUSH.BC
			defb $09,$09,$3B,$43,$20,$4F,$4E,$20				; $701B ..;C ON 
			defb $54,$48,$45,$20,$4C,$45,$46,$54				; $7023 THE LEFT
			defb $20,$41,$4E,$44,$20,$42,$20,$4F				; $702B  AND B O
			defb $4E,$20,$54,$48,$45,$20,$52,$49				; $7033 N THE RI
			defb $47,$48,$54,$0D,$0A,$09,$50,$55				; $703B GHT...PU
			defb $53,$48,$09,$44,$45,$0D,$0A,$09				; $7043 SH.DE...
			defb $50,$55,$53,$48,$09,$48,$4C,$0D				; $704B PUSH.HL.
			defb $0A,$09,$4C,$44,$09,$41,$2C,$45				; $7053 ..LD.A,E
			defb $0D,$0A,$09,$43,$50,$09,$31,$32				; $705B ...CP.12
			defb $38,$0D,$0A,$09,$4A,$FF                        ; $7063 8...J.

			LD HL,$6EC4				; $7069
			LD A,(HL)				; $706C
			LD B,A				; $706D
			CP $FF				; $706E
			RET Z				; $7070
			OR A				; $7071
			JR NZ,$707A				; $7072
			LD BC,$0006				; $7074
			ADD HL,BC				; $7077
			JR $706C				; $7078

			PUSH HL				; $707A
			INC HL				; $707B
			PUSH HL				; $707C
			LD E,(HL)				; $707D
			INC HL				; $707E
			LD D,(HL)				; $707F
			LD A,B				; $7080
			CALL $80BD				; $7081
			INC HL				; $7084
			LD A,E				; $7085
			ADD A,(HL)				; $7086
			LD E,A				; $7087
			CP $78				; $7088
			JR NC,$70AA				; $708A
			INC HL				; $708C
			INC (HL)				; $708D
			LD A,(HL)				; $708E
			ADD A,D				; $708F
			LD D,A				; $7090
			CP $B8				; $7091
			JR NC,$70AA				; $7093
			INC HL				; $7095
			LD C,(HL)				; $7096
			POP HL				; $7097
			LD (HL),E				; $7098
			INC HL				; $7099
			LD (HL),D				; $709A
			LD A,B				; $709B
			CALL $80BD				; $709C
			CALL $A4AD				; $709F
			POP HL				; $70A2
			LD BC,$0006				; $70A3
			ADD HL,BC				; $70A6
			JP $706C				; $70A7

			POP HL				; $70AA
			POP HL				; $70AB
			LD (HL),$00				; $70AC
			LD BC,$0006				; $70AE
			ADD HL,BC				; $70B1
			JR $706C				; $70B2

			LD A,$C3				; $70B4
			LD ($FDFD),A				; $70B6
			LD HL,$70D3				; $70B9
			LD ($FDFE),HL				; $70BC
			LD HL,$FE00				; $70BF
			LD (HL),$FD				; $70C2
			LD DE,$FE01				; $70C4
			LD BC,$0101				; $70C7
			LDIR				; $70CA
			IM 2				; $70CC
			LD A,$FE				; $70CE
			LD I,A				; $70D0
			RET				; $70D2

			PUSH AF				; $70D3
			PUSH BC				; $70D4
			PUSH DE				; $70D5
			PUSH HL				; $70D6
			PUSH IX				; $70D7
			PUSH IY				; $70D9
			EXX				; $70DB
			EX AF,AF'				; $70DC
			PUSH AF				; $70DD
			PUSH BC				; $70DE
			PUSH DE				; $70DF
			PUSH HL				; $70E0
			LD HL,$711A				; $70E1
			INC (HL)				; $70E4
			LD A,($830A)				; $70E5
			OR A				; $70E8
			CALL Z,$EF3B				; $70E9
			LD A,($830A)				; $70EC
			OR A				; $70EF
			JR Z,$7109				; $70F0
			LD A,($8599)				; $70F2
			OR A				; $70F5
			JR NZ,$7106				; $70F6
			LD A,($711B)				; $70F8
			XOR $01				; $70FB
			LD ($711B),A				; $70FD
			CALL Z,$85BA				; $7100
			JP $7109				; $7103

			CALL $EF38				; $7106
			POP HL				; $7109
			POP DE				; $710A
			POP BC				; $710B
			POP AF				; $710C
			EX AF,AF'				; $710D
			EXX				; $710E
			POP IY				; $710F
			POP IX				; $7111
			POP HL				; $7113
			POP DE				; $7114
			POP BC				; $7115
			POP AF				; $7116
			EI				; $7117
			RETI				; $7118

			defb $00                                            ; $711A .
			defb $00,$AA,$A5,$25,$A6,$82,$A6,$D3				; $711B ...%....
			defb $A6,$42,$A7,$BD,$A7,$C0,$A7,$2D				; $7123 .B.....-
			defb $A8,$A9,$A8,$03,$A9,$71,$A9,$CB				; $712B .....q..
			defb $A9,$CE,$A9,$32,$AA,$A1,$AA,$19				; $7133 ...2....
			defb $AB,$98,$AB,$03,$AC,$06,$AC,$95				; $713B ........
			defb $AC,$08,$AD,$83,$AD,$FB,$AD,$79				; $7143 .......y
			defb $AE,$D2,$AE,$40,$AF,$CD,$AF,$37				; $714B ...@...7
			defb $B0,$C6,$B0,$45,$B1,$B8,$B1,$2D				; $7153 ...E...-
			defb $B2,$86,$B2,$DA,$B2,$5B,$B3,$D8				; $715B .....[..
			defb $B3,$4C,$B4,$C4,$B4,$22,$B5,$8F				; $7163 .L..."..
			defb $B5,$06,$B6,$99,$B6,$9C,$B6,$16				; $716B ........
			defb $B7,$8B,$B7,$DA,$B7,$11,$B8,$4F				; $7173 .......O
			defb $B8,$C0,$B8,$28,$B9,$91,$B9,$FF				; $717B ...(....
			defb $B9,$62,$BA,$C9,$BA,$1D,$BB,$86				; $7183 .b......
			defb $BB,$18,$BC,$76,$BC,$D7,$BC,$3D				; $718B ...v...=
			defb $BD,$C9,$BD,$33,$BE,$B4,$BE,$16				; $7193 ...3....
			defb $BF,$5E,$BF,$CC,$BF,$3C,$C0,$AB				; $719B .^...<..
			defb $C0,$05,$C1,$A5,$C1,$45,$C2,$AC				; $71A3 .....E..
			defb $C2,$FD,$C2,$75,$C3,$91,$77,$00				; $71AB ...u..w.
			defb $00                                            ; $71B3 .

			LD A,E				; $71B4
			CP $78				; $71B5
			JR Z,$71C6				; $71B7
			OR A				; $71B9
			JR Z,$71C6				; $71BA
			LD A,($6971)				; $71BC
			CP D				; $71BF
			JP NZ,$71D6				; $71C0
			JP $71CD				; $71C3
			LD A,($6970)				; $71C6
			CP E				; $71C9
			JP NZ,$71D6				; $71CA
			POP HL				; $71CD
			INC HL				; $71CE
			INC HL				; $71CF
			INC HL				; $71D0
			LD A,$01				; $71D1
			OR A				; $71D3
			PUSH HL				; $71D4
			RET				; $71D5

			POP HL				; $71D6
			LD A,($696B)				; $71D7
			CP $FF				; $71DA
			LD A,E				; $71DC
			JR Z,$71E9				; $71DD
			CP $78				; $71DF
			JR NZ,$71F2				; $71E1
			LD A,$01				; $71E3
			LD E,$00				; $71E5
			JR $720E				; $71E7

			OR A				; $71E9
			JR NZ,$71F2				; $71EA
			LD A,$FF				; $71EC
			LD E,$78				; $71EE
			JR $720E				; $71F0

			LD A,($696C)				; $71F2
			CP $FE				; $71F5
			LD A,D				; $71F7
			JR Z,$7204				; $71F8
			CP $B0				; $71FA
			RET NZ				; $71FC
			LD A,($744C)				; $71FD
			LD D,$20				; $7200
			JR $720E				; $7202

			CP $20				; $7204
			RET NZ				; $7206
			LD A,($744C)				; $7207
			NEG				; $720A
			LD D,$B0				; $720C
			LD ($6969),DE				; $720E
			LD ($6970),DE				; $7212
			LD DE,($744B)				; $7216
			ADD A,E				; $721A
			LD ($744B),A				; $721B
			XOR A				; $721E
			LD ($7E94),A				; $721F
			LD ($800D),A				; $7222
			LD ($65EA),A				; $7225
			LD ($80B3),A				; $7228
			LD A,$FF				; $722B
			LD ($9B47),A				; $722D
			LD ($9BD5),A				; $7230
			LD A,$32				; $7233
			LD ($9BD4),A				; $7235
			LD HL,$0753				; $7238
			LD ($71B2),HL				; $723B
			LD HL,$C6F9				; $723E
			LD ($696D),HL				; $7241
			LD ($6AEF),HL				; $7244
			LD ($6AF4),HL				; $7247
			LD HL,$9142				; $724A
			LD (HL),$FF				; $724D
			LD ($9140),HL				; $724F
			LD HL,$921E				; $7252
			LD (HL),$FF				; $7255
			LD ($9265),HL				; $7257
			CALL $7457				; $725A
			CALL $8892				; $725D
			CALL $66C0				; $7260
			LD A,($744B)				; $7263
			LD BC,$711C				; $7266
			CALL $744D				; $7269
			LD DE,$2000				; $726C
			LD A,(HL)				; $726F
			INC HL				; $7270
			OR A				; $7271
			JR Z,$7284				; $7272
			CP $FF				; $7274
			JR Z,$729A				; $7276
			PUSH HL				; $7278
			CALL $A548				; $7279
			CALL $A57F				; $727C
			CALL $678F				; $727F
			LD (HL),A				; $7282
			POP HL				; $7283
			LD A,E				; $7284
			ADD A,$08				; $7285
			LD E,A				; $7287
			CP $80				; $7288
			JP NZ,$726F				; $728A
			LD A,D				; $728D
			CP $B0				; $728E
			JR Z,$72C8				; $7290
			ADD A,$10				; $7292
			LD D,A				; $7294
			LD E,$00				; $7295
			JP $726F				; $7297

			LD B,(HL)				; $729A
			INC HL				; $729B
			LD A,(HL)				; $729C
			INC HL				; $729D
			LD C,A				; $729E
			OR A				; $729F
			JR Z,$72AF				; $72A0
			PUSH HL				; $72A2
			CALL $A548				; $72A3
			CALL $A57F				; $72A6
			CALL $678F				; $72A9
			LD (HL),A				; $72AC
			LD C,A				; $72AD
			POP HL				; $72AE
			LD A,E				; $72AF
			ADD A,$08				; $72B0
			LD E,A				; $72B2
			CP $80				; $72B3
			JP NZ,$72C2				; $72B5
			LD A,D				; $72B8
			CP $B0				; $72B9
			JR Z,$72C8				; $72BB
			ADD A,$10				; $72BD
			LD D,A				; $72BF
			LD E,$00				; $72C0
			LD A,C				; $72C2
			DJNZ $729E				; $72C3
			JP $726F				; $72C5

			CALL $742E				; $72C8
			LD IX,$6CFC				; $72CB
			LD DE,$2000				; $72CF
			CALL $678F				; $72D2
			LD A,(HL)				; $72D5
			CP $E9				; $72D6
			JR C,$72DF				; $72D8
			LD (HL),$00				; $72DA
			JP $72EC				; $72DC

			OR A				; $72DF
			CALL NZ,$732A				; $72E0
			CALL $8748				; $72E3
			CALL $6E07				; $72E6
			CALL $73AA				; $72E9
			CALL $8FD6				; $72EC
			CALL $91B1				; $72EF
			CALL $9B68				; $72F2
			CALL $A1D6				; $72F5
			LD A,E				; $72F8
			ADD A,$08				; $72F9
			LD E,A				; $72FB
			CP $80				; $72FC
			JP NZ,$72D2				; $72FE
			LD A,D				; $7301
			CP $B0				; $7302
			JR Z,$730E				; $7304
			ADD A,$10				; $7306
			LD D,A				; $7308
			LD E,$00				; $7309
			JP $72D2				; $730B

			LD (IX+$00),$FF				; $730E
			CALL $67AD				; $7312
			CALL $9BD8				; $7315
			LD A,$01				; $7318
			LD ($9BD3),A				; $731A
			LD DE,($6969)				; $731D
			LD ($6AF1),DE				; $7321
			LD B,D				; $7325
			LD C,E				; $7326
			JP $6953				; $7327

			CP $E9				; $732A
			RET NC				; $732C
			PUSH AF				; $732D
			PUSH DE				; $732E
			PUSH HL				; $732F
			LD DE,$0005				; $7330
			LD B,A				; $7333
			LD HL,$736D				; $7334
			LD A,(HL)				; $7337
			ADD HL,DE				; $7338
			CP $FF				; $7339
			JR Z,$735B				; $733B
			CP B				; $733D
			JR NZ,$7337				; $733E
			SBC HL,DE				; $7340
			INC HL				; $7342
			EX DE,HL				; $7343
			POP HL				; $7344
			PUSH HL				; $7345
			LD A,(DE)				; $7346
			LD (HL),A				; $7347
			INC HL				; $7348
			INC DE				; $7349
			LD A,(DE)				; $734A
			LD (HL),A				; $734B
			INC DE				; $734C
			LD BC,$001F				; $734D
			ADD HL,BC				; $7350
			LD A,(DE)				; $7351
			LD (HL),A				; $7352
			INC HL				; $7353
			INC DE				; $7354
			LD A,(DE)				; $7355
			LD (HL),A				; $7356
			POP HL				; $7357
			POP DE				; $7358
			POP AF				; $7359
			RET				; $735A

			POP HL				; $735B
			PUSH HL				; $735C
			INC HL				; $735D
			LD (HL),$01				; $735E
			LD DE,$001F				; $7360
			ADD HL,DE				; $7363
			LD (HL),$01				; $7364
			INC HL				; $7366
			LD (HL),$01				; $7367
			POP HL				; $7369
			POP DE				; $736A
			POP AF				; $736B
			RET				; $736C

			defb $0F,$00,$00,$01,$01,$0D                        ; $736D ......
			defb $00,$00,$01,$01,$35,$01,$01,$00				; $7373 ....5...
			defb $00,$37,$01,$01,$00,$00,$80,$00				; $737B .7......
			defb $01,$01,$01,$82,$01,$00,$01,$01				; $7383 ........
			defb $83,$00,$00,$00,$00,$84,$00,$00				; $738B ........
			defb $00,$00,$89,$00,$00,$00,$00,$8A				; $7393 ........
			defb $00,$00,$00,$00,$95,$01,$00,$01				; $739B ........
			defb $00,$97,$00,$01,$00,$01,$FF                    ; $73A3 .......

			PUSH AF				; $73AA
			PUSH DE				; $73AB
			PUSH HL				; $73AC
			LD HL,$73F4				; $73AD
			LD B,A				; $73B0
			LD A,(HL)				; $73B1
			INC HL				; $73B2
			CP $FF				; $73B3
			JP Z,$73F0				; $73B5
			CP B				; $73B8
			JR Z,$73BF				; $73B9
			INC HL				; $73BB
			JP $73B1				; $73BC

			PUSH HL				; $73BF
			PUSH BC				; $73C0
			LD L,(HL)				; $73C1
			LD H,$00				; $73C2
			LD C,L				; $73C4
			LD B,H				; $73C5
			ADD HL,HL				; $73C6
			ADD HL,HL				; $73C7
			ADD HL,BC				; $73C8
			LD BC,$7413				; $73C9
			ADD HL,BC				; $73CC
			PUSH HL				; $73CD
			POP IY				; $73CE
			LD L,(IY+$02)				; $73D0
			LD H,(IY+$03)				; $73D3
			LD (HL),E				; $73D6
			INC HL				; $73D7
			LD (HL),D				; $73D8
			INC HL				; $73D9
			LD C,(IY+$04)				; $73DA
			DEC C				; $73DD
			DEC C				; $73DE
			LD B,$00				; $73DF
			ADD HL,BC				; $73E1
			LD (HL),$FF				; $73E2
			LD (IY+$02),L				; $73E4
			LD (IY+$03),H				; $73E7
			POP BC				; $73EA
			POP HL				; $73EB
			INC HL				; $73EC
			JP $73B1				; $73ED

			POP HL				; $73F0
			POP DE				; $73F1
			POP AF				; $73F2
			RET				; $73F3

			defb $2E,$00,$2F,$00,$8D,$00,$8E                    ; $73F4 ../....
			defb $00,$8F,$00,$90,$00,$27,$01,$32				; $73FB .....'.2
			defb $01,$52,$01,$94,$01,$98,$01,$46				; $7403 .R.....F
			defb $02,$47,$02,$81,$03,$83,$04,$FF				; $740B .G......
			defb $33,$75,$00,$00,$02,$1C,$8E,$00				; $7413 3u......
			defb $00,$02,$79,$98,$00,$00,$02,$A3				; $741B ..y.....
			defb $75,$00,$00,$02,$44,$76,$00,$00				; $7423 u...Dv..
			defb $02,$00,$00                                    ; $742B ...

			LD BC,$0005				; $742E
			LD IX,$7413				; $7431
			LD A,(IX+$00)				; $7435
			LD L,A				; $7438
			LD H,(IX+$01)				; $7439
			OR H				; $743C
			RET Z				; $743D
			LD (HL),$FF				; $743E
			LD (IX+$02),L				; $7440
			LD (IX+$03),H				; $7443
			ADD IX,BC				; $7446
			JP $7435				; $7448

			NOP				; $744B
			NOP				; $744C

			LD L,A				; $744D
			LD H,$00				; $744E
			ADD HL,HL				; $7450
			ADD HL,BC				; $7451
			LD A,(HL)				; $7452
			INC HL				; $7453
			LD H,(HL)				; $7454
			LD L,A				; $7455
			RET				; $7456

			LD HL,$5F00				; $7457
			LD DE,$5F01				; $745A
			LD BC,$03FF				; $745D
			LD (HL),$00				; $7460
			LDIR				; $7462
			RET				; $7464

			LD ($74E1),A				; $7465
			ADD A,A				; $7468
			ADD A,A				; $7469
			ADD A,A				; $746A
			LD L,A				; $746B
			LD H,$00				; $746C
			LD BC,$74C9				; $746E
			ADD HL,BC				; $7471
			LD E,(HL)				; $7472
			INC HL				; $7473
			LD D,(HL)				; $7474
			INC HL				; $7475
			LD ($6969),DE				; $7476
			LD ($6970),DE				; $747A
			LD A,(HL)				; $747E
			LD ($744C),A				; $747F
			INC HL				; $7482
			LD A,(HL)				; $7483
			LD ($744B),A				; $7484
			INC HL				; $7487
			LD E,(HL)				; $7488
			INC HL				; $7489
			LD D,(HL)				; $748A
			LD ($7A1D),DE				; $748B
			LD ($7A1B),DE				; $748F
			INC HL				; $7493
			LD A,(HL)				; $7494
			LD ($696B),A				; $7495
			XOR A				; $7498
			LD ($79D3),A				; $7499
			LD ($7643),A				; $749C
			LD ($6AEE),A				; $749F
			LD ($6AF3),A				; $74A2
			LD ($8F94),A				; $74A5
			LD ($711A),A				; $74A8
			LD HL,$0000				; $74AB
			LD ($7973),HL				; $74AE
			CALL $7935				; $74B1
			CALL $66AA				; $74B4
			CALL $77FF				; $74B7
			CALL $721E				; $74BA
			LD DE,($6969)				; $74BD
			LD B,D				; $74C1
			LD C,E				; $74C2
			CALL $6953				; $74C3
			JP $66E7				; $74C6

			defb $20,$90                                        ; $74C9  .
			defb $06,$00,$F4,$01,$01,$00,$60,$50				; $74CB ......`P
			defb $06,$15,$EE,$02,$FF,$00,$60,$60				; $74D3 ......``
			defb $08,$2B,$84,$03,$FF,$00,$00                    ; $74DB .+.....

			PUSH AF				; $74E2
			PUSH BC				; $74E3
			PUSH DE				; $74E4
			PUSH HL				; $74E5
			DEC D				; $74E6
			DEC D				; $74E7
			DEC D				; $74E8
			DEC D				; $74E9
			DEC E				; $74EA
			DEC E				; $74EB
			JP $74F3				; $74EC

			PUSH AF				; $74EF
			PUSH BC				; $74F0
			PUSH DE				; $74F1
			PUSH HL				; $74F2
			LD C,E				; $74F3
			LD B,D				; $74F4
			LD HL,$7533				; $74F5
			LD A,(HL)				; $74F8
			CP $FF				; $74F9
			JR NZ,$7502				; $74FB
			POP HL				; $74FD
			POP DE				; $74FE
			POP BC				; $74FF
			POP AF				; $7500
			RET				; $7501

			LD E,A				; $7502
			INC HL				; $7503
			LD D,(HL)				; $7504
			INC HL				; $7505
			PUSH HL				; $7506
			CALL $6802				; $7507
			OR A				; $750A
			JR Z,$752F				; $750B
			CALL $678F				; $750D
			LD A,(HL)				; $7510
			OR A				; $7511
			JR Z,$752F				; $7512
			CALL $88EA				; $7514
			LD A,(HL)				; $7517
			LD HL,$C6F9				; $7518
			CALL $A4DB				; $751B
			CALL $6DE0				; $751E
			LD DE,$7904				; $7521
			CALL $78D4				; $7524
			CALL $788E				; $7527
			LD A,$04				; $752A
			CALL $85B0				; $752C
			POP HL				; $752F
			JP $74F8				; $7530

			defb $45,$54,$52,$49,$45,$56,$45,$20				; $7533 ETRIEVE
			defb $4F,$4C,$44,$20,$43,$4F,$4F,$52				; $753B OLD COOR
			defb $44,$53,$0D,$0A,$09,$4C,$44,$09				; $7543 DS...LD.
			defb $41,$2C,$45,$0D,$0A,$09,$41,$4E				; $754B A,E...AN
			defb $44,$09,$30,$31,$31,$31,$31,$31				; $7553 D.011111
			defb $30,$30,$42,$0D,$0A,$09,$52,$52				; $755B 00B...RR
			defb $43,$41,$0D,$0A,$09,$52,$52,$43				; $7563 CA...RRC
			defb $41,$0D,$0A,$09,$4C,$44,$09,$28				; $756B A...LD.(
			defb $24,$33,$2B,$31,$29,$2C,$41,$0D				; $7573 $3+1),A.
			defb $0A,$09,$4C,$44,$09,$43,$2C,$44				; $757B ..LD.C,D
			defb $FF                                            ; $7583 .

			LD HL,$75A3				; $7584
			LD A,(HL)				; $7587
			CP $FF				; $7588
			RET Z				; $758A
			LD E,A				; $758B
			INC E				; $758C
			INC E				; $758D
			INC HL				; $758E
			LD D,(HL)				; $758F
			LD A,D				; $7590
			SUB $08				; $7591
			LD D,A				; $7593
			INC HL				; $7594
			LD A,$01				; $7595
			CALL $6679				; $7597
			ADD A,$42				; $759A
			LD C,A				; $759C
			CALL $6E8B				; $759D
			JP $7587				; $75A0

			defb $49,$54,$45,$20,$41,$44,$44,$52				; $75A3 ITE ADDR
			defb $45,$53,$53,$0D,$0A,$09,$45,$58				; $75AB ESS...EX
			defb $58,$0D,$0A,$0D,$FF                            ; $75B3 X....

			LD DE,($7644)				; $75B8
			LD A,E				; $75BC
			CP $FF				; $75BD
			RET Z				; $75BF
			LD A,($7643)				; $75C0
			OR A				; $75C3
			JP Z,$75DC				; $75C4
			DEC A				; $75C7
			LD ($7643),A				; $75C8
			RET NZ				; $75CB
			CALL $7647				; $75CC
			LD A,($74E1)				; $75CF
			INC A				; $75D2
			CP $03				; $75D3
			JP NZ,$7465				; $75D5
			XOR A				; $75D8
			JP $7465				; $75D9
			LD HL,($6969)				; $75DC
			LD A,D				; $75DF
			SUB $10				; $75E0
			CP H				; $75E2
			RET NC				; $75E3
			LD A,E				; $75E4
			INC L				; $75E5
			INC L				; $75E6
			CP L				; $75E7
			RET NC				; $75E8
			ADD A,$08				; $75E9
			DEC L				; $75EB
			DEC L				; $75EC
			DEC L				; $75ED
			DEC L				; $75EE
			CP L				; $75EF
			RET C				; $75F0
			LD HL,$C6F9				; $75F1
			LD A,$83				; $75F4
			CALL $A4DB				; $75F6
			LD A,$0C				; $75F9
			CALL $9FA2				; $75FB
			LD A,E				; $75FE
			ADD A,$08				; $75FF
			LD E,A				; $7601
			LD HL,$C6F9				; $7602
			LD A,$84				; $7605
			CALL $A4DB				; $7607
			LD A,$0D				; $760A
			CALL $9FA2				; $760C
			LD A,D				; $760F
			SUB $10				; $7610
			LD D,A				; $7612
			LD A,E				; $7613
			SUB $04				; $7614
			LD E,A				; $7616
			LD A,$0E				; $7617
			CALL $9FA2				; $7619
			LD A,$FF				; $761C
			LD ($8F94),A				; $761E
			LD A,$40				; $7621
			LD ($7643),A				; $7623
			LD DE,($6969)				; $7626
			LD B,D				; $762A
			LD C,E				; $762B
			LD HL,$C6F9				; $762C
			LD ($696D),HL				; $762F
			LD ($6AEF),HL				; $7632
			LD ($6AF4),HL				; $7635
			CALL $6953				; $7638
			XOR A				; $763B
			LD ($6AEE),A				; $763C
			LD ($6AF3),A				; $763F
			RET				; $7642

			defb $00,$00,$00,$FF                                ; $7643 ....

			CALL $66C0				; $7647
			XOR A				; $764A
			LD ($696A),A				; $764B
			LD A,$4A				; $764E
			LD ($744B),A				; $7650
			CALL $721E				; $7653
			LD DE,$1038				; $7656
			LD A,$0C				; $7659
			CALL $9FA2				; $765B
			LD A,E				; $765E
			ADD A,$08				; $765F
			LD E,A				; $7661
			LD A,$0D				; $7662
			CALL $9FA2				; $7664
			LD A,D				; $7667
			SUB $10				; $7668
			LD D,A				; $766A
			LD A,E				; $766B
			SUB $04				; $766C
			LD E,A				; $766E
			LD A,$0E				; $766F
			CALL $9FA2				; $7671
			LD B,$60				; $7674
			PUSH BC				; $7676
			HALT				; $7677
			HALT				; $7678
			CALL $A04E				; $7679
			CALL $A086				; $767C
			POP BC				; $767F
			DJNZ $7676				; $7680
			LD A,($79D3)				; $7682
			CP $11				; $7685
			JP NC,$76BB				; $7687
			LD HL,($7973)				; $768A
			LD DE,$05DC				; $768D
			AND A				; $7690
			SBC HL,DE				; $7691
			JR C,$76BB				; $7693
			LD HL,$796D				; $7695
			LD DE,$771A				; $7698
			LD BC,$0005				; $769B
			LDIR				; $769E
			LD HL,$76CC				; $76A0
			CALL $6B29				; $76A3
			LD HL,$78D3				; $76A6
			INC (HL)				; $76A9
			LD DE,$7972				; $76AA
			CALL $78D4				; $76AD
			CALL $688F				; $76B0
			LD A,($68AA)				; $76B3
			OR A				; $76B6
			JP Z,$76B0				; $76B7
			RET				; $76BA

			LD HL,$7730				; $76BB
			CALL $6B29				; $76BE
			CALL $688F				; $76C1
			LD A,($68AA)				; $76C4
			OR A				; $76C7
			JP Z,$76C1				; $76C8
			RET				; $76CB

			defb $E6,$F1,$C2,$E0,$45,$DF,$13                    ; $76CC ....E..
			defb $03,$57,$45,$4C,$4C,$20,$44,$4F				; $76D3 .WELL DO
			defb $4E,$45,$20,$43,$59,$42,$45,$52				; $76DB NE CYBER
			defb $4E,$4F,$49,$44,$20,$50,$49,$4C				; $76E3 NOID PIL
			defb $4F,$54,$21,$7A,$E4,$59,$4F,$55				; $76EB OT!z.YOU
			defb $52,$20,$53,$4B,$49,$4C,$4C,$20				; $76F3 R SKILL 
			defb $48,$41,$53,$20,$45,$41,$52,$4E				; $76FB HAS EARN
			defb $45,$44,$20,$41,$4E,$4F,$54,$48				; $7703 ED ANOTH
			defb $45,$52,$7A,$E3,$43,$52,$41,$46				; $770B ERz.CRAF
			defb $54,$20,$41,$4E,$44,$20,$DD,$30				; $7713 T AND .0
			defb $30,$30,$30,$30,$30,$DC,$20,$42				; $771B 00000. B
			defb $4F,$4E,$55,$53,$20,$50,$4F,$49				; $7723 ONUS POI
			defb $4E,$54,$53,$2E,$FF,$E6,$F1,$C2				; $772B NTS.....
			defb $E0,$43,$DF,$13,$03,$59,$4F,$55				; $7733 .C...YOU
			defb $20,$48,$41,$56,$45,$20,$46,$41				; $773B  HAVE FA
			defb $49,$4C,$45,$44,$20,$54,$4F,$20				; $7743 ILED TO 
			defb $52,$45,$54,$52,$45,$49,$56,$45				; $774B RETREIVE
			defb $7A,$E2,$41,$20,$43,$41,$52,$47				; $7753 z.A CARG
			defb $4F,$20,$56,$41,$4C,$55,$45,$20				; $775B O VALUE 
			defb $4F,$46,$20,$31,$35,$30,$30,$20				; $7763 OF 1500 
			defb $57,$49,$54,$48,$49,$4E,$20,$54				; $776B WITHIN T
			defb $48,$45,$7A,$E4,$54,$49,$4D,$45				; $7773 HEz.TIME
			defb $20,$41,$4C,$4C,$4F,$43,$41,$54				; $777B  ALLOCAT
			defb $45,$44,$20,$2D,$20,$42,$41,$44				; $7783 ED - BAD
			defb $20,$4C,$55,$43,$4B,$FF,$FF,$06				; $778B  LUCK...
			defb $00,$87,$00,$00,$87,$FF,$0C,$00				; $7793 ........
			defb $87,$00,$00,$87,$FF,$09,$00,$0B				; $779B ........
			defb $00,$00,$8B,$00,$00,$8B,$FF,$09				; $77A3 ........
			defb $00,$0A,$00,$00,$87,$00,$00,$87				; $77AB ........
			defb $FF,$09,$00,$09,$00,$00,$87,$00				; $77B3 ........
			defb $00,$87,$00,$46,$46,$FF,$05,$00				; $77BB ...FF...
			defb $06,$07,$08,$00,$88,$89,$8A,$88				; $77C3 ........
			defb $00,$4C,$4D,$46,$00,$00,$FF,$05				; $77CB .LMF....
			defb $14,$9A,$FF,$04,$9B,$9C,$FF,$05				; $77D3 ........
			defb $14,$FF,$30,$00,$E5,$CD,$44,$11				; $77DB ..0...D.
			defb $C2,$B5,$0A,$CD,$EF,$0B,$CD,$E1				; $77E3 ........
			defb $0B,$E1,$18,$E8,$19,$11,$00,$40				; $77EB .......@
			defb $19,$E5,$36,$FA,$23,$35,$23,$35				; $77F3 ..6.#5#5
			defb $97,$32,$6E,$0A                                ; $77FB .2n.

			LD HL,$7814				; $77FF
			CALL $6B29				; $7802
			CALL $788E				; $7805
			CALL $78AF				; $7808
			CALL $78C8				; $780B
			CALL $79A9				; $780E
			JP $7B72				; $7811

			defb $DF,$00,$00,$E6,$D1,$C5,$DE                    ; $7814 .......
			defb $18,$DC,$E4,$1E,$19,$DE,$1A,$DF				; $781B ........
			defb $03,$00,$DE,$1B,$DC,$E4,$1E,$1C				; $7823 ........
			defb $DE,$1D,$DF,$01,$01,$DE,$00,$01				; $782B ........
			defb $AD,$02,$03,$DF,$01,$06,$DC,$0A				; $7833 ........
			defb $DB,$0B,$AD,$0C,$D3,$0D,$DF,$01				; $783B ........
			defb $0F,$DE,$04,$DD,$05,$AD,$DD,$06				; $7843 ........
			defb $D5,$07,$78,$02,$DA,$08,$D2,$09				; $784B ..x.....
			defb $DF,$01,$18,$DA,$0E,$D2,$0F,$AD				; $7853 ........
			defb $10,$D9,$11,$DF,$01,$00,$DC,$1E				; $785B ........
			defb $AE,$1E,$DF,$01,$1F,$DC,$1E,$AE				; $7863 ........
			defb $1E,$DF,$00,$05,$E3,$7F,$78,$DF				; $786B ......x.
			defb $00,$0E,$E3,$7F,$78,$DF,$00,$17				; $7873 ....x...
			defb $E3,$7F,$78,$FF,$DE,$1F,$D4,$23				; $787B ..x....#
			defb $AD,$DE,$20,$AE,$D6,$21,$AE,$22				; $7883 .. ..!."
			defb $D4,$24,$FF                                    ; $788B .$.

			PUSH AF				; $788E
			PUSH BC				; $788F
			PUSH DE				; $7890
			PUSH HL				; $7891
			LD C,$45				; $7892
			LD HL,$C2F1				; $7894
			LD ($6C55),HL				; $7897
			LD HL,$78F9				; $789A
			LD DE,$0108				; $789D
			LD B,$06				; $78A0
			LD A,(HL)				; $78A2
			CALL $6C4A				; $78A3
			INC E				; $78A6
			INC HL				; $78A7
			DJNZ $78A2				; $78A8
			POP HL				; $78AA
			POP DE				; $78AB
			POP BC				; $78AC
			POP AF				; $78AD
			RET				; $78AE

			LD HL,$C2F1				; $78AF
			LD ($6C55),HL				; $78B2
			LD C,$43				; $78B5
			LD HL,$796C				; $78B7
			LD DE,$0208				; $78BA
			LD B,$06				; $78BD
			LD A,(HL)				; $78BF
			CALL $6C4A				; $78C0
			INC E				; $78C3
			INC HL				; $78C4
			DJNZ $78BF				; $78C5
			RET				; $78C7

			LD A,($78D3)				; $78C8
			LD DE,$0203				; $78CB
			LD C,$46				; $78CE
			JP $7977				; $78D0

			NOP				; $78D3

			PUSH AF				; $78D4
			PUSH BC				; $78D5
			PUSH DE				; $78D6
			PUSH HL				; $78D7
			LD C,$00				; $78D8
			LD HL,$78FE				; $78DA
			LD B,$06				; $78DD
			LD A,(DE)				; $78DF
			ADD A,(HL)				; $78E0
			SUB $30				; $78E1
			ADD A,C				; $78E3
			CP $3A				; $78E4
			LD C,$01				; $78E6
			JR C,$78EE				; $78E8
			SUB $0A				; $78EA
			JR $78EF				; $78EC

			DEC C				; $78EE
			LD (HL),A				; $78EF
			DEC HL				; $78F0
			DEC DE				; $78F1
			DJNZ $78DF				; $78F2
			POP HL				; $78F4
			POP DE				; $78F5
			POP BC				; $78F6
			POP AF				; $78F7
			RET				; $78F8

			defb $30,$30                                        ; $78F9 00
			defb $30,$30,$30,$30,$30,$30,$30,$30				; $78FB 00000000
			defb $32,$35,$30,$30,$30,$31,$30,$30				; $7903 25000100
			defb $30,$30,$30,$32,$35,$30,$30,$30				; $790B 00025000
			defb $30,$35,$30,$30,$30,$30,$31,$30				; $7913 05000010
			defb $30,$30,$30,$30,$32,$30,$30,$30				; $791B 00002000
			defb $30,$30,$35,$30,$30,$30,$30,$31				; $7923 00500001
			defb $30,$30,$30,$30,$30,$32,$30,$30				; $792B 00000200
			defb $30,$30                                        ; $7933 00

			LD HL,($7973)				; $7935
			LD IX,$796D				; $7938
			LD IY,$7962				; $793C
			LD A,$30				; $7940
			LD E,(IY+$00)				; $7942
			LD D,(IY+$01)				; $7945
			OR A				; $7948
			SBC HL,DE				; $7949
			JR C,$7950				; $794B
			INC A				; $794D
			JR $7948				; $794E

			ADD HL,DE				; $7950
			LD (IX+$00),A				; $7951
			INC IX				; $7954
			INC IY				; $7956
			INC IY				; $7958
			LD A,(IX+$00)				; $795A
			CP $FF				; $795D
			RET Z				; $795F
			JR $7940				; $7960

			defb $10                                            ; $7962 .
			defb $27,$E8,$03,$64,$00,$0A,$00,$01				; $7963 '..d....
			defb $00,$30,$30,$30,$30,$30,$30,$FF				; $796B .000000.
			defb $00,$00,$00,$00                                ; $7973 ....

			PUSH BC				; $7977
			PUSH HL				; $7978
			LD HL,$C2F1				; $7979
			LD ($6C55),HL				; $797C
			LD B,$64				; $797F
			CALL $7991				; $7981
			LD B,$0A				; $7984
			CALL $7991				; $7986
			LD B,$01				; $7989
			CALL $7991				; $798B
			POP HL				; $798E
			POP BC				; $798F
			RET				; $7990

			LD L,$00				; $7991
			SUB B				; $7993
			JR C,$7999				; $7994
			INC L				; $7996
			JR $7993				; $7997

			ADD A,B				; $7999
			PUSH AF				; $799A
			LD A,B				; $799B
			CP $64				; $799C
			JR Z,$79A7				; $799E
			LD A,L				; $79A0
			ADD A,$30				; $79A1
			CALL $6C4A				; $79A3
			INC E				; $79A6
			POP AF				; $79A7
			RET				; $79A8

			LD HL,$79BE				; $79A9
			CALL $6B29				; $79AC
			LD A,($79D3)				; $79AF
			OR A				; $79B2
			RET Z				; $79B3
			LD E,$7B				; $79B4
			LD B,A				; $79B6
			CALL $79D4				; $79B7
			DEC E				; $79BA
			DJNZ $79B7				; $79BB
			RET				; $79BD

			defb $DF,$01,$1D,$E6,$D1                            ; $79BE .....
			defb $C5,$E0,$05,$12,$DC,$13,$AB,$D3				; $79C3 ........
			defb $14,$DB,$15,$D4,$16,$DC,$17,$FF				; $79CB ........
			defb $00                                            ; $79D3 .

			PUSH AF				; $79D4
			PUSH BC				; $79D5
			PUSH HL				; $79D6
			LD A,E				; $79D7
			AND $03				; $79D8
			LD L,A				; $79DA
			LD H,$00				; $79DB
			LD BC,$79FA				; $79DD
			ADD HL,BC				; $79E0
			LD C,(HL)				; $79E1
			LD HL,$4020				; $79E2
			LD A,E				; $79E5
			AND $FC				; $79E6
			RRCA				; $79E8
			RRCA				; $79E9
			ADD A,L				; $79EA
			LD L,A				; $79EB
			LD B,$10				; $79EC
			LD A,C				; $79EE
			AND (HL)				; $79EF
			LD (HL),A				; $79F0
			CALL $674C				; $79F1
			DJNZ $79EE				; $79F4
			POP HL				; $79F6
			POP BC				; $79F7
			POP AF				; $79F8
			RET				; $79F9

			defb $3F                                            ; $79FA ?
			defb $CF,$F3,$FC                                    ; $79FB ...

			LD HL,($7A1B)				; $79FE
			DEC HL				; $7A01
			LD ($7A1B),HL				; $7A02
			LD A,L				; $7A05
			OR H				; $7A06
			RET NZ				; $7A07
			LD HL,($7A1D)				; $7A08
			LD ($7A1B),HL				; $7A0B
			LD A,($79D3)				; $7A0E
			CP $11				; $7A11
			RET Z				; $7A13
			INC A				; $7A14
			LD ($79D3),A				; $7A15
			JP $79A9				; $7A18

			defb $00,$00,$00,$00                                ; $7A1B ....

			PUSH AF				; $7A1F
			PUSH BC				; $7A20
			PUSH HL				; $7A21
			LD A,E				; $7A22
			AND $03				; $7A23
			LD L,A				; $7A25
			LD H,$00				; $7A26
			LD BC,$7A4C				; $7A28
			ADD HL,BC				; $7A2B
			LD C,(HL)				; $7A2C
			CALL $673B				; $7A2D
			LD A,C				; $7A30
			XOR (HL)				; $7A31
			LD (HL),A				; $7A32
			LD A,H				; $7A33
			RRCA				; $7A34
			RRCA				; $7A35
			RRCA				; $7A36
			AND $03				; $7A37
			LD H,A				; $7A39
			LD BC,$5B00				; $7A3A
			ADD HL,BC				; $7A3D
			LD A,(HL)				; $7A3E
			OR A				; $7A3F
			JR NZ,$7A48				; $7A40
			LD BC,$FD00				; $7A42
			ADD HL,BC				; $7A45
			LD (HL),$47				; $7A46
			POP HL				; $7A48
			POP BC				; $7A49
			POP AF				; $7A4A
			RET				; $7A4B

			RET NZ				; $7A4C
			JR NC,$7A5B				; $7A4D
			INC BC				; $7A4F
			LD A,($8F94)				; $7A50
			OR A				; $7A53
			RET NZ				; $7A54
			LD A,($68AA)				; $7A55
			OR A				; $7A58
			JR NZ,$7A5F				; $7A59
			LD ($7AE9),A				; $7A5B
			RET				; $7A5E

			LD A,($7AE9)				; $7A5F
			OR A				; $7A62
			JR Z,$7A6B				; $7A63
			INC A				; $7A65
			RET Z				; $7A66
			LD ($7AE9),A				; $7A67
			RET				; $7A6A

			INC A				; $7A6B
			LD ($7AE9),A				; $7A6C
			LD DE,($6969)				; $7A6F
			LD A,($7AB4)				; $7A73
			XOR $04				; $7A76
			LD ($7AB4),A				; $7A78
			ADD A,D				; $7A7B
			LD D,A				; $7A7C
			LD A,($696B)				; $7A7D
			CP $FF				; $7A80
			LD A,$05				; $7A82
			JR NZ,$7A88				; $7A84
			LD A,$01				; $7A86
			ADD A,E				; $7A88
			LD E,A				; $7A89
			LD A,($696B)				; $7A8A
			ADD A,A				; $7A8D
			ADD A,A				; $7A8E
			CALL $7AB5				; $7A8F
			LD A,($6AEE)				; $7A92
			OR A				; $7A95
			RET Z				; $7A96
			LD DE,($6969)				; $7A97
			LD A,($696B)				; $7A9B
			CP $FF				; $7A9E
			LD A,$0E				; $7AA0
			LD C,$04				; $7AA2
			JR Z,$7AAA				; $7AA4
			LD A,$FA				; $7AA6
			LD C,$FC				; $7AA8
			ADD A,E				; $7AAA
			LD E,A				; $7AAB
			LD A,D				; $7AAC
			ADD A,$04				; $7AAD
			LD D,A				; $7AAF
			LD A,C				; $7AB0
			JP $7AB5				; $7AB1

			LD A,(BC)				; $7AB4
			EX AF,AF'				; $7AB5
			LD A,E				; $7AB6
			CP $7C				; $7AB7
			RET NC				; $7AB9
			LD HL,$7B36				; $7ABA
			BIT 7,(HL)				; $7ABD
			RET NZ				; $7ABF
			INC HL				; $7AC0
			INC HL				; $7AC1
			LD A,(HL)				; $7AC2
			INC HL				; $7AC3
			OR A				; $7AC4
			JR NZ,$7ABD				; $7AC5
			EX AF,AF'				; $7AC7
			DEC HL				; $7AC8
			LD (HL),A				; $7AC9
			DEC HL				; $7ACA
			LD (HL),D				; $7ACB
			DEC HL				; $7ACC
			LD (HL),E				; $7ACD
			LD A,$05				; $7ACE
			CALL $85B0				; $7AD0
			PUSH HL				; $7AD3
			PUSH DE				; $7AD4
			LD E,$1A				; $7AD5
			CALL $EF42				; $7AD7
			POP DE				; $7ADA
			CALL $678F				; $7ADB
			LD A,(HL)				; $7ADE
			OR (HL)				; $7ADF
			POP HL				; $7AE0
			JP Z,$7A1F				; $7AE1
			INC HL				; $7AE4
			INC HL				; $7AE5
			LD (HL),$00				; $7AE6
			RET				; $7AE8

			NOP				; $7AE9
			LD A,$03				; $7AEA
			CALL $67B9				; $7AEC
			LD HL,$7B36				; $7AEF
			LD E,(HL)				; $7AF2
			BIT 7,E				; $7AF3
			RET NZ				; $7AF5
			INC HL				; $7AF6
			LD D,(HL)				; $7AF7
			INC HL				; $7AF8
			LD A,(HL)				; $7AF9
			INC HL				; $7AFA
			OR A				; $7AFB
			JR Z,$7AF2				; $7AFC
			LD C,E				; $7AFE
			ADD A,E				; $7AFF
			CP $7C				; $7B00
			JR NC,$7B27				; $7B02
			EX AF,AF'				; $7B04
			PUSH HL				; $7B05
			CALL $678F				; $7B06
			LD A,(HL)				; $7B09
			POP HL				; $7B0A
			OR A				; $7B0B
			JR NZ,$7B27				; $7B0C
			CALL $9A75				; $7B0E
			JP NZ,$7B27				; $7B11
			EX AF,AF'				; $7B14
			LD E,A				; $7B15
			DEC HL				; $7B16
			DEC HL				; $7B17
			DEC HL				; $7B18
			LD (HL),A				; $7B19
			INC HL				; $7B1A
			INC HL				; $7B1B
			INC HL				; $7B1C
			CALL $7A1F				; $7B1D
			LD E,C				; $7B20
			CALL $7A1F				; $7B21
			JP $7AF2				; $7B24

			CALL $7A1F				; $7B27
			DEC HL				; $7B2A
			LD (HL),$00				; $7B2B
			INC HL				; $7B2D
			CALL $8ABF				; $7B2E
			CALL $74EF				; $7B31
			JR $7AF2				; $7B34

			defb $0A,$09,$4C,$44,$09                            ; $7B36 ..LD.
			defb $48,$2C,$30,$0D,$0A,$09,$41,$44				; $7B3B H,0...AD
			defb $44,$09,$48,$4C,$2C,$FF,$00                    ; $7B43 D.HL,..

			LD A,$F7				; $7B4A
			IN A,($FE)				; $7B4C
			AND $1F				; $7B4E
			CP $1F				; $7B50
			JR NZ,$7B59				; $7B52
			XOR A				; $7B54
			LD ($7B49),A				; $7B55
			RET				; $7B58

			LD D,A				; $7B59
			LD A,($7B49)				; $7B5A
			OR A				; $7B5D
			RET NZ				; $7B5E
			INC A				; $7B5F
			LD ($7B49),A				; $7B60
			LD A,D				; $7B63
			LD BC,$0500				; $7B64
			RRCA				; $7B67
			JR NC,$7B6E				; $7B68
			INC C				; $7B6A
			DJNZ $7B67				; $7B6B
			RET				; $7B6D

			LD A,C				; $7B6E
			LD ($7BFA),A				; $7B6F
			LD C,$47				; $7B72
			LD A,($7BFA)				; $7B74
			LD HL,$C2F1				; $7B77
			LD ($6C55),HL				; $7B7A
			ADD A,A				; $7B7D
			ADD A,A				; $7B7E
			ADD A,A				; $7B7F
			ADD A,A				; $7B80
			LD L,A				; $7B81
			LD H,$00				; $7B82
			LD DE,$7BAA				; $7B84
			ADD HL,DE				; $7B87
			LD DE,$0111				; $7B88
			LD B,$06				; $7B8B
			LD A,(HL)				; $7B8D
			CALL $6C4A				; $7B8E
			INC HL				; $7B91
			INC E				; $7B92
			DJNZ $7B8D				; $7B93
			LD DE,$0005				; $7B95
			ADD HL,DE				; $7B98
			LD DE,$0211				; $7B99
			LD A,(HL)				; $7B9C
			CALL $7977				; $7B9D
			INC HL				; $7BA0
			INC HL				; $7BA1
			INC HL				; $7BA2
			INC HL				; $7BA3
			LD A,(HL)				; $7BA4
			LD E,$15				; $7BA5
			JP $7977				; $7BA7

			defb $42                                            ; $7BAA B
			defb $4F,$4D,$42,$53,$20,$20,$20,$20				; $7BAB OMBS    
			defb $20,$20,$00,$5C,$7C,$14,$14,$4D				; $7BB3   .\|..M
			defb $49,$4E,$45,$53,$20,$20,$20,$20				; $7BBB INES    
			defb $20,$20,$00,$CE,$7D,$14,$14,$53				; $7BC3   ..}..S
			defb $48,$49,$45,$4C,$44,$20,$20,$20				; $7BCB HIELD   
			defb $20,$20,$00,$76,$7E,$01,$01,$42				; $7BD3   .v~..B
			defb $4F,$55,$4E,$43,$45,$20,$20,$20				; $7BDB OUNCE   
			defb $20,$20,$00,$95,$7E,$05,$05,$53				; $7BE3   ..~..S
			defb $45,$45,$4B,$45,$52,$20,$20,$20				; $7BEB EEKER   
			defb $20,$20,$00,$0E,$80,$05,$05,$00				; $7BF3   ......
			defb $00                                            ; $7BFB .

			LD DE,$0010				; $7BFC
			LD B,$05				; $7BFF
			LD IX,$7BAA				; $7C01
			LD A,(IX+$0E)				; $7C05
			LD (IX+$0B),A				; $7C08
			ADD IX,DE				; $7C0B
			DJNZ $7C05				; $7C0D
			RET				; $7C0F

			LD A,($8F94)				; $7C10
			OR A				; $7C13
			RET NZ				; $7C14
			LD DE,($6969)				; $7C15
			LD A,E				; $7C19
			CP $79				; $7C1A
			RET NC				; $7C1C
			LD A,D				; $7C1D
			CP $B1				; $7C1E
			RET NC				; $7C20
			CP $20				; $7C21
			RET C				; $7C23
			LD A,($7AE9)				; $7C24
			CP $05				; $7C27
			RET C				; $7C29
			LD HL,($7BFA)				; $7C2A
			ADD HL,HL				; $7C2D
			ADD HL,HL				; $7C2E
			ADD HL,HL				; $7C2F
			ADD HL,HL				; $7C30
			LD BC,$7BB5				; $7C31
			ADD HL,BC				; $7C34
			LD A,(HL)				; $7C35
			OR A				; $7C36
			RET Z				; $7C37
			PUSH HL				; $7C38
			INC HL				; $7C39
			LD A,(HL)				; $7C3A
			INC HL				; $7C3B
			LD H,(HL)				; $7C3C
			LD L,A				; $7C3D
			XOR A				; $7C3E
			LD ($7C54),A				; $7C3F
			JP (HL)				; $7C42
			POP HL				; $7C43
			LD A,($7C54)				; $7C44
			OR A				; $7C47
			RET Z				; $7C48
			LD A,(HL)				; $7C49
			DEC A				; $7C4A
			LD (HL),A				; $7C4B
			LD DE,$0211				; $7C4C
			LD C,$47				; $7C4F
			JP $7977				; $7C51

			NOP				; $7C54
			XOR A				; $7C55
			LD ($7C54),A				; $7C56
			JP $7C43				; $7C59

			LD HL,$7D75				; $7C5C
			LD A,($696C)				; $7C5F
			CP $02				; $7C62
			JP Z,$7C6A				; $7C64
			LD HL,$7D85				; $7C67
			LD IX,$7D95				; $7C6A
			LD A,($65EA)				; $7C6E
			AND $03				; $7C71
			JP NZ,$7C43				; $7C73
			LD BC,$0007				; $7C76
			LD A,(IX+$00)				; $7C79
			CP $FF				; $7C7C
			JP Z,$7C43				; $7C7E
			LD A,(IX+$02)				; $7C81
			OR A				; $7C84
			JR Z,$7C8C				; $7C85
			ADD IX,BC				; $7C87
			JP $7C79				; $7C89

			LD A,$02				; $7C8C
			CALL $85B0				; $7C8E
			PUSH DE				; $7C91
			PUSH HL				; $7C92
			PUSH IX				; $7C93
			LD E,$18				; $7C95
			CALL $EF42				; $7C97
			POP IX				; $7C9A
			POP HL				; $7C9C
			POP DE				; $7C9D
			LD (IX+$02),$10				; $7C9E
			LD (IX+$03),L				; $7CA2
			LD (IX+$04),H				; $7CA5
			LD HL,($6969)				; $7CA8
			LD DE,$0402				; $7CAB
			ADD HL,DE				; $7CAE
			LD (IX+$00),L				; $7CAF
			LD (IX+$01),H				; $7CB2
			LD A,($696B)				; $7CB5
			ADD A,A				; $7CB8
			LD (IX+$05),A				; $7CB9
			EX DE,HL				; $7CBC
			CP $FE				; $7CBD
			LD A,$01				; $7CBF
			JR Z,$7CC4				; $7CC1
			XOR A				; $7CC3
			LD (IX+$06),A				; $7CC4
			CALL $80BD				; $7CC7
			LD A,$01				; $7CCA
			LD ($7C54),A				; $7CCC
			JP $7C43				; $7CCF

			LD A,$02				; $7CD2
			CALL $67B9				; $7CD4
			LD IX,$7D95				; $7CD7
			LD A,(IX+$00)				; $7CDB
			CP $FF				; $7CDE
			RET Z				; $7CE0
			LD A,(IX+$02)				; $7CE1
			OR A				; $7CE4
			JR NZ,$7CEF				; $7CE5
			LD BC,$0007				; $7CE7
			ADD IX,BC				; $7CEA
			JP $7CDB				; $7CEC

			LD C,(IX+$02)				; $7CEF
			LD B,$00				; $7CF2
			LD L,(IX+$03)				; $7CF4
			LD H,(IX+$04)				; $7CF7
			DEC HL				; $7CFA
			ADD HL,BC				; $7CFB
			DEC C				; $7CFC
			JR Z,$7D02				; $7CFD
			LD (IX+$02),C				; $7CFF
			LD E,(IX+$00)				; $7D02
			LD D,(IX+$01)				; $7D05
			LD A,C				; $7D08
			CP $0B				; $7D09
			JR NC,$7D15				; $7D0B
			LD A,($65EA)				; $7D0D
			AND $01				; $7D10
			CALL Z,$8ABF				; $7D12
			LD A,(IX+$06)				; $7D15
			CALL $80BD				; $7D18
			LD A,(IX+$05)				; $7D1B
			LD B,A				; $7D1E
			PUSH HL				; $7D1F
			CALL $678F				; $7D20
			LD A,(HL)				; $7D23
			POP HL				; $7D24
			OR A				; $7D25
			JR NZ,$7D58				; $7D26
			LD A,B				; $7D28
			ADD A,E				; $7D29
			CP $7C				; $7D2A
			JR NC,$7D58				; $7D2C
			LD E,A				; $7D2E
			LD (IX+$00),A				; $7D2F
			LD A,(IX+$01)				; $7D32
			LD D,A				; $7D35
			CALL $815D				; $7D36
			JR NZ,$7D58				; $7D39
			LD A,(HL)				; $7D3B
			ADD A,D				; $7D3C
			LD D,A				; $7D3D
			CP $B8				; $7D3E
			JR NC,$7D58				; $7D40
			CALL $9A75				; $7D42
			JR NZ,$7D58				; $7D45
			LD (IX+$01),D				; $7D47
			LD A,(IX+$06)				; $7D4A
			LD C,$47				; $7D4D
			CALL $A4AD				; $7D4F
			CALL $80BD				; $7D52
			JP $7CE7				; $7D55

			XOR A				; $7D58
			LD (IX+$02),A				; $7D59
			CALL $6C9A				; $7D5C
			LD A,$01				; $7D5F
			CALL $67B9				; $7D61
			CALL $74E2				; $7D64
			CALL $74E2				; $7D67
			CALL $74E2				; $7D6A
			LD A,$02				; $7D6D
			CALL $67B9				; $7D6F
			JP $7CE7				; $7D72

			defb $06,$04,$04,$03,$03,$03                        ; $7D75 ......
			defb $02,$02,$02,$01,$01,$01,$00,$00				; $7D7B ........
			defb $00,$00,$FA,$FC,$FC,$FD,$FD,$FD				; $7D83 ........
			defb $FE,$FE,$FE,$FF,$FF,$FF,$00,$00				; $7D8B ........
			defb $00,$00,$C2,$16,$6C,$DD,$56,$0D				; $7D93 ....l.V.
			defb $DD,$5E,$0C,$DD,$72,$05,$DD,$73				; $7D9B .^..r..s
			defb $04,$C9,$00,$00,$00,$00,$00,$00				; $7DA3 ........
			defb $00,$38,$00,$00,$00,$64,$00,$0A				; $7DAB .8...d..
			defb $00,$00,$00,$00,$0A,$00,$00,$00				; $7DB3 ........
			defb $04,$00,$00,$00,$12,$00,$00,$00				; $7DBB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $7DC3 ........
			defb $00,$00,$FF                                    ; $7DCB ...

			LD DE,($6969)				; $7DCE
			INC D				; $7DD2
			INC D				; $7DD3
			INC D				; $7DD4
			INC D				; $7DD5
			INC E				; $7DD6
			INC E				; $7DD7
			XOR A				; $7DD8
			CALL $67B9				; $7DD9
			LD HL,$7E57				; $7DDC
			LD A,(HL)				; $7DDF
			CP $FF				; $7DE0
			JR Z,$7DF7				; $7DE2
			LD C,A				; $7DE4
			INC HL				; $7DE5
			LD B,(HL)				; $7DE6
			INC HL				; $7DE7
			LD A,(HL)				; $7DE8
			INC HL				; $7DE9
			OR A				; $7DEA
			JR Z,$7DDF				; $7DEB
			CALL $6802				; $7DED
			OR A				; $7DF0
			JP NZ,$7C43				; $7DF1
			JP $7DDF				; $7DF4

			LD HL,$7E57				; $7DF7
			LD A,(HL)				; $7DFA
			CP $FF				; $7DFB
			JP Z,$7C43				; $7DFD
			INC HL				; $7E00
			INC HL				; $7E01
			LD A,(HL)				; $7E02
			OR A				; $7E03
			JR Z,$7E0A				; $7E04
			INC HL				; $7E06
			JP $7DFA				; $7E07

			LD (HL),$01				; $7E0A
			DEC HL				; $7E0C
			LD (HL),D				; $7E0D
			DEC HL				; $7E0E
			LD (HL),E				; $7E0F
			LD A,$04				; $7E10
			CALL $80BD				; $7E12
			LD ($7C54),A				; $7E15
			LD A,$03				; $7E18
			CALL $85B0				; $7E1A
			PUSH DE				; $7E1D
			LD E,$1F				; $7E1E
			CALL $EF42				; $7E20
			POP DE				; $7E23
			JP $7C43				; $7E24

			LD A,$02				; $7E27
			CALL $67B9				; $7E29
			LD C,$47				; $7E2C
			LD HL,$7E57				; $7E2E
			LD A,(HL)				; $7E31
			CP $FF				; $7E32
			RET Z				; $7E34
			LD E,A				; $7E35
			INC HL				; $7E36
			LD D,(HL)				; $7E37
			INC HL				; $7E38
			LD A,(HL)				; $7E39
			INC HL				; $7E3A
			OR A				; $7E3B
			JP Z,$7E31				; $7E3C
			CALL $A4AD				; $7E3F
			CALL $9A75				; $7E42
			JP Z,$7E31				; $7E45
			DEC HL				; $7E48
			LD (HL),$00				; $7E49
			INC HL				; $7E4B
			LD A,$04				; $7E4C
			CALL $80BD				; $7E4E
			CALL $8ABF				; $7E51
			JP $7E31				; $7E54

			defb $A1,$06,$42,$06                                ; $7E57 ..B.
			defb $E8,$05,$93,$05,$43,$05,$F7,$04				; $7E5B ....C...
			defb $B0,$04,$6D,$04,$2D,$04,$F1,$03				; $7E63 ..m.-...
			defb $B8,$03,$83,$03,$50,$03,$21,$03				; $7E6B ....P.!.
			defb $F4,$02,$FF                                    ; $7E73 ...

			LD A,($7E94)				; $7E76
			OR A				; $7E79
			JP NZ,$7C43				; $7E7A
			LD A,$04				; $7E7D
			CALL $85B0				; $7E7F
			PUSH DE				; $7E82
			LD E,$16				; $7E83
			CALL $EF42				; $7E85
			POP DE				; $7E88
			LD A,$5A				; $7E89
			LD ($7E94),A				; $7E8B
			LD ($7C54),A				; $7E8E
			JP $7C43				; $7E91

			NOP				; $7E94

			LD A,($800D)				; $7E95
			OR A				; $7E98
			JP NZ,$7C43				; $7E99
			LD A,$96				; $7E9C
			LD ($800D),A				; $7E9E
			LD A,$01				; $7EA1
			CALL $85B0				; $7EA3
			PUSH DE				; $7EA6
			LD E,$1C				; $7EA7
			CALL $EF42				; $7EA9
			POP DE				; $7EAC
			LD DE,($6969)				; $7EAD
			LD A,E				; $7EB1
			AND $FC				; $7EB2
			LD E,A				; $7EB4
			LD A,D				; $7EB5
			AND $F8				; $7EB6
			LD D,A				; $7EB8
			LD C,$02				; $7EB9
			LD B,$04				; $7EBB
			CALL $7EE4				; $7EBD
			LD C,$FE				; $7EC0
			LD B,$04				; $7EC2
			LD A,E				; $7EC4
			ADD A,$04				; $7EC5
			LD E,A				; $7EC7
			CALL $7EE4				; $7EC8
			LD C,$FE				; $7ECB
			LD B,$FC				; $7ECD
			LD A,D				; $7ECF
			ADD A,$08				; $7ED0
			LD D,A				; $7ED2
			CALL $7EE4				; $7ED3
			LD C,$02				; $7ED6
			LD B,$FC				; $7ED8
			LD A,E				; $7EDA
			SUB $04				; $7EDB
			LD E,A				; $7EDD
			CALL $7EE4				; $7EDE
			JP $7C43				; $7EE1

			LD HL,$7F08				; $7EE4
			LD A,(HL)				; $7EE7
			CP $FF				; $7EE8
			RET Z				; $7EEA
			INC HL				; $7EEB
			INC HL				; $7EEC
			INC HL				; $7EED
			INC HL				; $7EEE
			LD A,(HL)				; $7EEF
			INC HL				; $7EF0
			OR A				; $7EF1
			JR NZ,$7EE7				; $7EF2
			DEC HL				; $7EF4
			LD (HL),$01				; $7EF5
			DEC HL				; $7EF7
			LD (HL),B				; $7EF8
			DEC HL				; $7EF9
			LD (HL),C				; $7EFA
			DEC HL				; $7EFB
			LD (HL),D				; $7EFC
			DEC HL				; $7EFD
			LD (HL),E				; $7EFE
			LD A,$05				; $7EFF
			CALL $80BD				; $7F01
			LD ($7C54),A				; $7F04
			RET				; $7F07

			defb $01,$CE,$01                                    ; $7F08 ...
			defb $CF,$01,$D9,$01,$E3,$01,$ED,$01				; $7F0B ........
			defb $F7,$01,$01,$02,$0B,$02,$12,$02				; $7F13 ........
			defb $28,$FF                                        ; $7F1B (.

			LD A,$02				; $7F1D
			CALL $67B9				; $7F1F
			LD HL,$7F08				; $7F22
			LD A,(HL)				; $7F25
			CP $FF				; $7F26
			RET Z				; $7F28
			LD E,(HL)				; $7F29
			INC HL				; $7F2A
			LD D,(HL)				; $7F2B
			INC HL				; $7F2C
			LD C,(HL)				; $7F2D
			INC HL				; $7F2E
			LD B,(HL)				; $7F2F
			INC HL				; $7F30
			LD A,(HL)				; $7F31
			INC HL				; $7F32
			OR A				; $7F33
			JR Z,$7F25				; $7F34
			LD A,C				; $7F36
			CP $FE				; $7F37
			JR Z,$7F45				; $7F39
			CALL $8115				; $7F3B
			OR A				; $7F3E
			CALL NZ,$7F97				; $7F3F
			JP $7F4C				; $7F42

			CALL $8139				; $7F45
			OR A				; $7F48
			CALL NZ,$7F97				; $7F49
			LD A,B				; $7F4C
			CP $FC				; $7F4D
			JR Z,$7F5B				; $7F4F
			CALL $815D				; $7F51
			OR A				; $7F54
			CALL NZ,$7FBF				; $7F55
			JP $7F62				; $7F58

			CALL $8181				; $7F5B
			OR A				; $7F5E
			CALL NZ,$7FBF				; $7F5F
			LD A,$05				; $7F62
			CALL $80BD				; $7F64
			LD A,C				; $7F67
			ADD A,E				; $7F68
			LD E,A				; $7F69
			LD A,B				; $7F6A
			ADD A,D				; $7F6B
			LD D,A				; $7F6C
			PUSH HL				; $7F6D
			DEC HL				; $7F6E
			DEC HL				; $7F6F
			LD (HL),B				; $7F70
			DEC HL				; $7F71
			LD (HL),C				; $7F72
			DEC HL				; $7F73
			LD (HL),D				; $7F74
			DEC HL				; $7F75
			LD (HL),E				; $7F76
			POP HL				; $7F77
			LD A,$05				; $7F78
			CALL $80BD				; $7F7A
			LD C,$47				; $7F7D
			CALL $A4AD				; $7F7F
			CALL $9A75				; $7F82
			LD A,($800D)				; $7F85
			OR A				; $7F88
			JP Z,$7F25				; $7F89
			CP $0A				; $7F8C
			JP NC,$7F25				; $7F8E
			CALL $8ABF				; $7F91
			JP $7F25				; $7F94

			LD A,C				; $7F97
			NEG				; $7F98
			LD C,A				; $7F9A
			CALL $8ABF				; $7F9B
			LD A,$01				; $7F9E
			CALL $67B9				; $7FA0
			CALL $74E2				; $7FA3
			CALL $74E2				; $7FA6
			CALL $74E2				; $7FA9
			LD A,$02				; $7FAC
			CALL $67B9				; $7FAE
			PUSH BC				; $7FB1
			PUSH DE				; $7FB2
			PUSH HL				; $7FB3
			LD E,$1D				; $7FB4
			CALL $EF42				; $7FB6
			POP HL				; $7FB9
			POP DE				; $7FBA
			POP BC				; $7FBB
			JP $6C9A				; $7FBC

			LD A,B				; $7FBF
			NEG				; $7FC0
			LD B,A				; $7FC2
			CALL $8ABF				; $7FC3
			LD A,$01				; $7FC6
			CALL $67B9				; $7FC8
			CALL $74E2				; $7FCB
			CALL $74E2				; $7FCE
			CALL $74E2				; $7FD1
			LD A,$02				; $7FD4
			CALL $67B9				; $7FD6
			PUSH BC				; $7FD9
			PUSH DE				; $7FDA
			PUSH HL				; $7FDB
			LD E,$1D				; $7FDC
			CALL $EF42				; $7FDE
			POP HL				; $7FE1
			POP DE				; $7FE2
			POP BC				; $7FE3
			JP $6C9A				; $7FE4

			LD A,($800D)				; $7FE7
			OR A				; $7FEA
			RET Z				; $7FEB
			DEC A				; $7FEC
			LD ($800D),A				; $7FED
			RET NZ				; $7FF0
			LD HL,$7F08				; $7FF1
			LD A,(HL)				; $7FF4
			CP $FF				; $7FF5
			RET Z				; $7FF7
			LD E,A				; $7FF8
			INC HL				; $7FF9
			LD D,(HL)				; $7FFA
			INC HL				; $7FFB
			INC HL				; $7FFC
			INC HL				; $7FFD
			LD A,(HL)				; $7FFE
			LD (HL),$00				; $7FFF
			INC HL				; $8001
			OR A				; $8002
			JR Z,$7FF4				; $8003
			LD A,$05				; $8005
			CALL $80BD				; $8007
			JP $7FF4				; $800A

			NOP				; $800D
			LD IX,$80B1				; $800E
			LD A,(IX+$02)				; $8012
			OR A				; $8015
			JP NZ,$7C43				; $8016
			LD BC,$0008				; $8019
			LD IY,$6CFC				; $801C
			LD A,(IY+$00)				; $8020
			CP $FF				; $8023
			JR NZ,$8038				; $8025
			LD A,$78				; $8027
			CALL $6679				; $8029
			LD C,A				; $802C
			LD A,$90				; $802D
			CALL $6679				; $802F
			ADD A,$20				; $8032
			LD B,A				; $8034
			JP $8049				; $8035

			LD A,(IY+$04)				; $8038
			OR A				; $803B
			JR NZ,$8043				; $803C
			ADD IY,BC				; $803E
			JP $8020				; $8040

			LD C,(IY+$00)				; $8043
			LD B,(IY+$01)				; $8046
			LD HL,$C6F9				; $8049
			LD (IX+$0A),L				; $804C
			LD (IX+$0B),H				; $804F
			LD DE,($6969)				; $8052
			CALL $8B71				; $8056
			LD A,$01				; $8059
			LD ($7C54),A				; $805B
			LD E,$17				; $805E
			CALL $EF42				; $8060
			JP $7C43				; $8063

			LD IX,$80B1				; $8066
			LD A,(IX+$02)				; $806A
			OR A				; $806D
			RET Z				; $806E
			LD C,(IX+$00)				; $806F
			LD B,(IX+$01)				; $8072
			LD H,$05				; $8075
			CALL $8BCA				; $8077
			JR Z,$809E				; $807A
			DEC H				; $807C
			JR NZ,$8077				; $807D
			LD L,(IX+$0A)				; $807F
			LD H,(IX+$0B)				; $8082
			LD A,$11				; $8085
			CALL $A1FD				; $8087
			INC D				; $808A
			INC D				; $808B
			INC D				; $808C
			INC D				; $808D
			INC E				; $808E
			INC E				; $808F
			CALL $8ABF				; $8090
			LD (IX+$0A),L				; $8093
			LD (IX+$0B),H				; $8096
			LD C,$47				; $8099
			JP $A446				; $809B

			LD L,(IX+$0A)				; $809E
			LD H,(IX+$0B)				; $80A1
			XOR A				; $80A4
			CALL $A1FD				; $80A5
			INC E				; $80A8
			INC E				; $80A9
			INC D				; $80AA
			INC D				; $80AB
			INC D				; $80AC
			INC D				; $80AD
			JP $6C9A				; $80AE

			defb $01,$02                                        ; $80AB ..
			defb $67,$01,$C8,$80,$C8,$FF                        ; $80B3 g.....

			LD BC,$018F				; $80B9
			EX AF,AF'				; $80BC
			PUSH AF				; $80BD
			PUSH BC				; $80BE
			PUSH DE				; $80BF
			PUSH HL				; $80C0
			ADD A,A				; $80C1
			ADD A,A				; $80C2
			ADD A,A				; $80C3
			ADD A,A				; $80C4
			LD L,A				; $80C5
			LD H,$00				; $80C6
			ADD HL,HL				; $80C8
			ADD HL,HL				; $80C9
			LD C,D				; $80CA
			LD A,E				; $80CB
			AND $7C				; $80CC
			RRCA				; $80CE
			RRCA				; $80CF
			LD ($80EF),A				; $80D0
			LD ($8102),A				; $80D3
			LD A,E				; $80D6
			AND $03				; $80D7
			ADD A,A				; $80D9
			ADD A,A				; $80DA
			ADD A,A				; $80DB
			ADD A,A				; $80DC
			LD E,A				; $80DD
			LD D,$00				; $80DE
			ADD HL,DE				; $80E0
			LD DE,$EC61				; $80E1
			ADD HL,DE				; $80E4
			EX DE,HL				; $80E5
			LD B,$04				; $80E6
			LD H,$64				; $80E8
			LD L,C				; $80EA
			LD A,(HL)				; $80EB
			DEC H				; $80EC
			LD H,(HL)				; $80ED
			OR $00				; $80EE
			LD L,A				; $80F0
			INC C				; $80F1
			LD A,(DE)				; $80F2
			INC DE				; $80F3
			XOR (HL)				; $80F4
			LD (HL),A				; $80F5
			INC L				; $80F6
			LD A,(DE)				; $80F7
			INC DE				; $80F8
			XOR (HL)				; $80F9
			LD (HL),A				; $80FA
			LD H,$64				; $80FB
			LD L,C				; $80FD
			LD A,(HL)				; $80FE
			DEC H				; $80FF
			LD H,(HL)				; $8100
			OR $00				; $8101
			LD L,A				; $8103
			INC C				; $8104
			LD A,(DE)				; $8105
			INC DE				; $8106
			XOR (HL)				; $8107
			LD (HL),A				; $8108
			INC L				; $8109
			LD A,(DE)				; $810A
			INC DE				; $810B
			XOR (HL)				; $810C
			LD (HL),A				; $810D
			DJNZ $80E8				; $810E
			POP HL				; $8110
			POP DE				; $8111
			POP BC				; $8112
			POP AF				; $8113
			RET				; $8114

			PUSH BC				; $8115
			PUSH DE				; $8116
			PUSH HL				; $8117
			LD A,E				; $8118
			CP $7A				; $8119
			JR NC,$8134				; $811B
			AND $03				; $811D
			LD A,$00				; $811F
			JR NZ,$8134				; $8121
			CALL $678F				; $8123
			INC L				; $8126
			LD BC,$0020				; $8127
			LD A,(HL)				; $812A
			LD E,A				; $812B
			LD A,D				; $812C
			AND $07				; $812D
			LD A,E				; $812F
			JR Z,$8134				; $8130
			ADD HL,BC				; $8132
			OR (HL)				; $8133
			OR A				; $8134
			POP HL				; $8135
			POP DE				; $8136
			POP BC				; $8137
			RET				; $8138

			PUSH BC				; $8139
			PUSH DE				; $813A
			PUSH HL				; $813B
			LD A,E				; $813C
			CP $04				; $813D
			JR C,$8158				; $813F
			AND $03				; $8141
			LD A,$00				; $8143
			JR NZ,$8158				; $8145
			CALL $678F				; $8147
			DEC L				; $814A
			LD BC,$0020				; $814B
			LD A,(HL)				; $814E
			LD E,A				; $814F
			LD A,D				; $8150
			AND $07				; $8151
			LD A,E				; $8153
			JR Z,$8158				; $8154
			ADD HL,BC				; $8156
			OR (HL)				; $8157
			OR A				; $8158
			POP HL				; $8159
			POP DE				; $815A
			POP BC				; $815B
			RET				; $815C

			PUSH BC				; $815D
			PUSH DE				; $815E
			PUSH HL				; $815F
			LD A,D				; $8160
			CP $B8				; $8161
			JR NC,$817C				; $8163
			AND $07				; $8165
			LD A,$00				; $8167
			JR NZ,$817C				; $8169
			CALL $678F				; $816B
			LD BC,$0020				; $816E
			ADD HL,BC				; $8171
			LD A,(HL)				; $8172
			LD D,A				; $8173
			LD A,E				; $8174
			AND $03				; $8175
			LD A,D				; $8177
			JR Z,$817C				; $8178
			INC L				; $817A
			OR (HL)				; $817B
			OR A				; $817C
			POP HL				; $817D
			POP DE				; $817E
			POP BC				; $817F
			RET				; $8180

			PUSH BC				; $8181
			PUSH DE				; $8182
			PUSH HL				; $8183
			LD A,D				; $8184
			CP $20				; $8185
			JR C,$81A0				; $8187
			AND $07				; $8189
			LD A,$00				; $818B
			JR NZ,$81A0				; $818D
			CALL $678F				; $818F
			LD BC,$FFE0				; $8192
			ADD HL,BC				; $8195
			LD A,(HL)				; $8196
			LD D,A				; $8197
			LD A,E				; $8198
			AND $03				; $8199
			LD A,D				; $819B
			JR Z,$81A0				; $819C
			INC L				; $819E
			OR (HL)				; $819F
			OR A				; $81A0
			POP HL				; $81A1
			POP DE				; $81A2
			POP BC				; $81A3
			RET				; $81A4

			NOP				; $81A5
			CALL $66AA				; $81A6
			CALL $844F				; $81A9
			LD HL,$8235				; $81AC
			CALL $6B29				; $81AF
			LD A,($8599)				; $81B2
			OR A				; $81B5
			JR NZ,$81CB				; $81B6
			LD A,($81A5)				; $81B8
			OR A				; $81BB
			JR NZ,$81CB				; $81BC
			DI				; $81BE
			CALL $FB86				; $81BF
			CALL $FB8D				; $81C2
			EI				; $81C5
			LD A,$01				; $81C6
			LD ($81A5),A				; $81C8
			CALL $6672				; $81CB
			JP NZ,$81CB				; $81CE
			LD BC,$01F4				; $81D1
			PUSH BC				; $81D4
			CALL $84F5				; $81D5
			CALL $82E5				; $81D8
			CALL $662B				; $81DB
			POP BC				; $81DE
			CP $31				; $81DF
			JR C,$8229				; $81E1
			CP $37				; $81E3
			JR NC,$8229				; $81E5
			CP $31				; $81E7
			RET Z				; $81E9
			CP $32				; $81EA
			JP Z,$830C				; $81EC
			CP $36				; $81EF
			JR NZ,$8214				; $81F1
			LD A,($830A)				; $81F3
			XOR $01				; $81F6
			LD ($830A),A				; $81F8
			PUSH BC				; $81FB
			CALL $EF3B				; $81FC
			LD E,$01				; $81FF
			CALL $EF42				; $8201
			POP BC				; $8204
			CALL $82E5				; $8205
			CALL $6672				; $8208
			CALL NZ,$84F5				; $820B
			JP NZ,$8205				; $820E
			JP $8229				; $8211

			SUB $33				; $8214
			LD E,A				; $8216
			LD A,($68AB)				; $8217
			CP E				; $821A
			JR Z,$8229				; $821B
			LD A,E				; $821D
			LD ($68AB),A				; $821E
			PUSH BC				; $8221
			LD HL,$8235				; $8222
			CALL $6B29				; $8225
			POP BC				; $8228
			DEC BC				; $8229
			LD A,B				; $822A
			OR C				; $822B
			JP NZ,$81D4				; $822C
			CALL $9433				; $822F
			JP $81A6				; $8232

			defb $EB,$00,$E6,$F1,$C2,$DF                        ; $8235 ......
			defb $09,$08,$E0,$45,$42,$59,$20,$52				; $823B ...EBY R
			defb $41,$46,$46,$41,$45,$4C,$45,$20				; $8243 AFFAELE 
			defb $43,$45,$43,$43,$4F,$DA,$DF,$0B				; $824B CECCO...
			defb $06,$4D,$55,$53,$49,$43,$20,$42				; $8253 .MUSIC B
			defb $59,$20,$44,$41,$56,$45,$20,$52				; $825B Y DAVE R
			defb $4F,$47,$45,$52,$53,$DF,$0D,$09				; $8263 OGERS...
			defb $DA,$31,$20,$DB,$53,$54,$41,$52				; $826B .1 .STAR
			defb $54,$20,$47,$41,$4D,$45,$79,$F4				; $8273 T GAMEy.
			defb $DA,$32,$20,$DB,$44,$45,$46,$49				; $827B .2 .DEFI
			defb $4E,$45,$20,$4B,$45,$59,$53,$79				; $8283 NE KEYSy
			defb $F3,$DA,$33,$20,$DB,$4B,$45,$59				; $828B ..3 .KEY
			defb $42,$4F,$41,$52,$44,$79,$F6,$DA				; $8293 BOARDy..
			defb $34,$20,$DB,$49,$4E,$54,$45,$52				; $829B 4 .INTER
			defb $46,$41,$43,$45,$20,$32,$79,$F3				; $82A3 FACE 2y.
			defb $DA,$35,$20,$DB,$4B,$45,$4D,$50				; $82AB .5 .KEMP
			defb $53,$54,$4F,$4E,$79,$F6,$DA,$36				; $82B3 STONy..6
			defb $20,$DB,$53,$4F,$55,$4E,$44,$20				; $82BB  .SOUND 
			defb $4F,$4E,$2F,$4F,$46,$46,$DF,$14				; $82C3 ON/OFF..
			defb $04,$D9,$43,$59,$42,$45,$52,$4E				; $82CB ..CYBERN
			defb $4F,$49,$44,$20,$2A,$20,$31,$39				; $82D3 OID * 19
			defb $38,$38,$20,$48,$45,$57,$53,$4F				; $82DB 88 HEWSO
			defb $4E,$FF                                        ; $82E3 N.

			PUSH BC				; $82E5
			LD A,($68AB)				; $82E6
			ADD A,$0F				; $82E9
			ADD A,A				; $82EB
			ADD A,A				; $82EC
			ADD A,A				; $82ED
			LD D,A				; $82EE
			LD E,$2C				; $82EF
			CALL $6799				; $82F1
			LD A,($830B)				; $82F4
			INC A				; $82F7
			LD ($830B),A				; $82F8
			AND $07				; $82FB
			ADD A,$40				; $82FD
			LD (HL),A				; $82FF
			LD E,L				; $8300
			LD D,H				; $8301
			INC E				; $8302
			LD BC,$000F				; $8303
			LDIR				; $8306
			POP BC				; $8308
			RET				; $8309

			LD BC,$CD00				; $830A
			XOR D				; $830D
			LD H,(HL)				; $830E
			CALL $844F				; $830F
			XOR A				; $8312
			LD ($68AB),A				; $8313
			LD HL,$83FA				; $8316
			CALL $6B29				; $8319
			LD IX,$681D				; $831C
			LD IY,$83F1				; $8320
			LD DE,$0C0F				; $8324
			LD B,$04				; $8327
			PUSH BC				; $8329
			LD A,$3F				; $832A
			LD C,$44				; $832C
			CALL $6C4A				; $832E
			PUSH DE				; $8331
			CALL $6672				; $8332
			CALL NZ,$84F5				; $8335
			JP NZ,$8332				; $8338
			CALL $662B				; $833B
			OR A				; $833E
			CALL Z,$84F5				; $833F
			JP Z,$833B				; $8342
			LD (IX+$02),D				; $8345
			LD (IX+$06),E				; $8348
			LD DE,$000A				; $834B
			ADD IX,DE				; $834E
			LD (IY+$00),A				; $8350
			INC IY				; $8353
			POP DE				; $8355
			LD HL,$83C1				; $8356
			CP $20				; $8359
			JR NZ,$8360				; $835B
			LD HL,$83C5				; $835D
			CP $0D				; $8360
			JR NZ,$8367				; $8362
			LD HL,$83CD				; $8364
			CP $01				; $8367
			JR NZ,$836E				; $8369
			LD HL,$83D5				; $836B
			CP $02				; $836E
			JR NZ,$8375				; $8370
			LD HL,$83E2				; $8372
			LD ($83C1),A				; $8375
			LD C,$44				; $8378
			CALL $6B29				; $837A
			POP BC				; $837D
			DJNZ $8329				; $837E
			LD BC,$C350				; $8380
			CALL $676A				; $8383
			CALL $676A				; $8386
			LD HL,$83F1				; $8389
			LD DE,$83F6				; $838C
			LD B,$04				; $838F
			LD A,(DE)				; $8391
			CP (HL)				; $8392
			JP NZ,$81A6				; $8393
			INC HL				; $8396
			INC DE				; $8397
			DJNZ $8391				; $8398
			LD A,($8F4F)				; $839A
			XOR $35				; $839D
			LD ($8F4F),A				; $839F
			JP NZ,$81A6				; $83A2
			LD A,$04				; $83A5
			CALL $85B0				; $83A7
			LD E,$22				; $83AA
			CALL $EF42				; $83AC
			LD B,$64				; $83AF
			CALL $84F5				; $83B1
			DJNZ $83B1				; $83B4
			CALL $EF3B				; $83B6
			LD E,$01				; $83B9
			CALL $EF42				; $83BB
			JP $81A6				; $83BE

			defb $3F,$7A                                        ; $83C1 ?z
			defb $FF,$FF,$53,$50,$41,$43,$45,$7A				; $83C3 ..SPACEz
			defb $FB,$FF,$45,$4E,$54,$45,$52,$7A				; $83CB ..ENTERz
			defb $FB,$FF,$43,$41,$50,$53,$20,$53				; $83D3 ..CAPS S
			defb $48,$49,$46,$54,$7A,$F6,$FF,$53				; $83DB HIFTz..S
			defb $59,$4D,$42,$4F,$4C,$20,$53,$48				; $83E3 YMBOL SH
			defb $49,$46,$54,$7A,$F4,$FF,$00,$00				; $83EB IFTz....
			defb $00,$00,$00,$59,$58,$45,$53,$E0				; $83F3 ...YXES.
			defb $43,$DF,$09,$07,$E6,$F1,$C2,$53				; $83FB C......S
			defb $45,$4C,$45,$43,$54,$20,$4B,$45				; $8403 ELECT KE
			defb $59,$20,$46,$4F,$52,$2E,$2E,$2E				; $840B Y FOR...
			defb $2E,$DC,$7B,$EE,$4C,$45,$46,$54				; $8413 ..{.LEFT
			defb $7A,$FC,$52,$49,$47,$48,$54,$7A				; $841B z.RIGHTz
			defb $FB,$55,$50,$20,$20,$7A,$FC,$46				; $8423 .UP  z.F
			defb $49,$52,$45,$70,$04,$E5,$09,$20				; $842B IREp... 
			defb $DF,$14,$04,$D9,$43,$59,$42,$45				; $8433 ....CYBE
			defb $52,$4E,$4F,$49,$44,$20,$2A,$20				; $843B RNOID * 
			defb $31,$39,$38,$38,$20,$48,$45,$57				; $8443 1988 HEW
			defb $53,$4F,$4E,$FF                                ; $844B SON.

			LD DE,$0000				; $844F
			LD A,$10				; $8452
			CALL $A548				; $8454
			CALL $A57F				; $8457
			LD DE,$0078				; $845A
			LD A,$11				; $845D
			CALL $A548				; $845F
			CALL $A57F				; $8462
			LD DE,$B000				; $8465
			LD A,$12				; $8468
			CALL $A548				; $846A
			CALL $A57F				; $846D
			LD DE,$B078				; $8470
			LD A,$13				; $8473
			CALL $A548				; $8475
			CALL $A57F				; $8478
			LD DE,$1000				; $847B
			CALL $84B5				; $847E
			LD DE,$1078				; $8481
			CALL $84B5				; $8484
			LD DE,$0008				; $8487
			CALL $84C6				; $848A
			LD DE,$B008				; $848D
			CALL $84C6				; $8490
			LD DE,$101C				; $8493
			LD HL,$84D7				; $8496
			LD C,$03				; $8499
			LD B,$0A				; $849B
			LD A,(HL)				; $849D
			INC HL				; $849E
			CALL $A548				; $849F
			CALL $A57F				; $84A2
			LD A,E				; $84A5
			ADD A,$08				; $84A6
			LD E,A				; $84A8
			DJNZ $849D				; $84A9
			LD E,$1C				; $84AB
			LD A,D				; $84AD
			ADD A,$10				; $84AE
			LD D,A				; $84B0
			DEC C				; $84B1
			JR NZ,$849B				; $84B2
			RET				; $84B4

			LD B,$0A				; $84B5
			LD A,$15				; $84B7
			CALL $A548				; $84B9
			CALL $A57F				; $84BC
			LD A,D				; $84BF
			ADD A,$10				; $84C0
			LD D,A				; $84C2
			DJNZ $84B7				; $84C3
			RET				; $84C5

			LD B,$0E				; $84C6
			LD A,$14				; $84C8
			CALL $A548				; $84CA
			CALL $A57F				; $84CD
			LD A,E				; $84D0
			ADD A,$08				; $84D1
			LD E,A				; $84D3
			DJNZ $84C8				; $84D4
			RET				; $84D6

			defb $00,$6D,$6E,$00                                ; $84D7 .mn.
			defb $00,$00,$00,$00,$6F,$70,$5A,$5B				; $84DB ....opZ[
			defb $5C,$5D,$5E,$5F,$60,$61,$62,$63				; $84E3 \]^_`abc
			defb $00,$64,$65,$66,$67,$68,$69,$6A				; $84EB .defghij
			defb $6B,$6C,$76                                    ; $84F3 klv

			PUSH AF				; $84F6
			PUSH BC				; $84F7
			PUSH DE				; $84F8
			PUSH HL				; $84F9
			AND A				; $84FA
			LD HL,$4102				; $84FB
			LD D,$0E				; $84FE
			PUSH HL				; $8500
			LD B,$04				; $8501
			RR (HL)				; $8503
			INC L				; $8505
			RR (HL)				; $8506
			INC L				; $8508
			RR (HL)				; $8509
			INC L				; $850B
			RR (HL)				; $850C
			INC L				; $850E
			RR (HL)				; $850F
			INC L				; $8511
			RR (HL)				; $8512
			INC L				; $8514
			RR (HL)				; $8515
			INC L				; $8517
			DJNZ $8503				; $8518
			POP HL				; $851A
			JR NC,$851F				; $851B
			SET 7,(HL)				; $851D
			CALL $674C				; $851F
			DEC D				; $8522
			JP NZ,$8500				; $8523
			LD HL,$4040				; $8526
			LD DE,$4140				; $8529
			LD B,(HL)				; $852C
			INC HL				; $852D
			LD C,(HL)				; $852E
			DEC HL				; $852F
			EX DE,HL				; $8530
			PUSH BC				; $8531
			LD BC,$013E				; $8532
			LDI				; $8535
			LDI				; $8537
			EX AF,AF'				; $8539
			DEC L				; $853A
			DEC L				; $853B
			LD E,L				; $853C
			LD D,H				; $853D
			CALL $674C				; $853E
			EX AF,AF'				; $8541
			JP PE,$8535				; $8542
			POP BC				; $8545
			EX DE,HL				; $8546
			LD (HL),B				; $8547
			INC HL				; $8548
			LD (HL),C				; $8549
			LD DE,$57BE				; $854A
			LD BC,$0140				; $854D
			LD HL,$4040				; $8550
			LDI				; $8553
			LDI				; $8555
			EX AF,AF'				; $8557
			DEC DE				; $8558
			DEC DE				; $8559
			DEC HL				; $855A
			DEC HL				; $855B
			CALL $674C				; $855C
			EX DE,HL				; $855F
			CALL $675B				; $8560
			EX DE,HL				; $8563
			EX AF,AF'				; $8564
			JP PE,$8553				; $8565
			AND A				; $8568
			LD HL,$51DD				; $8569
			LD D,$0E				; $856C
			PUSH HL				; $856E
			LD B,$04				; $856F
			RL (HL)				; $8571
			DEC L				; $8573
			RL (HL)				; $8574
			DEC L				; $8576
			RL (HL)				; $8577
			DEC L				; $8579
			RL (HL)				; $857A
			DEC L				; $857C
			RL (HL)				; $857D
			DEC L				; $857F
			RL (HL)				; $8580
			DEC L				; $8582
			RL (HL)				; $8583
			DEC L				; $8585
			DJNZ $8571				; $8586
			POP HL				; $8588
			JR NC,$858D				; $8589
			SET 0,(HL)				; $858B
			CALL $674C				; $858D
			DEC D				; $8590
			JP NZ,$856E				; $8591
			POP HL				; $8594
			POP DE				; $8595
			POP BC				; $8596
			POP AF				; $8597
			RET				; $8598

			defb $01,$00                                        ; $8599 ..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $859B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $85A3 ........
			defb $00,$00,$00,$00,$00                            ; $85AB .....

			PUSH HL				; $85B0
			LD HL,$859B				; $85B1
			CP (HL)				; $85B4
			JR NC,$85B8				; $85B5
			LD (HL),A				; $85B7
			POP HL				; $85B8
			RET				; $85B9

			LD HL,$859A				; $85BA
			LD A,($859B)				; $85BD
			CP $FF				; $85C0
			JR Z,$861D				; $85C2
			LD C,A				; $85C4
			LD A,$FF				; $85C5
			LD ($859B),A				; $85C7
			LD A,(HL)				; $85CA
			AND A				; $85CB
			JR Z,$85D1				; $85CC
			CP C				; $85CE
			JR C,$861D				; $85CF
			LD (HL),C				; $85D1
			LD A,C				; $85D2
			AND A				; $85D3
			JP Z,$870B				; $85D4
			DEC A				; $85D7
			RLCA				; $85D8
			RLCA				; $85D9
			RLCA				; $85DA
			LD E,A				; $85DB
			LD D,$00				; $85DC
			LD HL,$8710				; $85DE
			ADD HL,DE				; $85E1
			LD DE,$859E				; $85E2
			LD BC,$0007				; $85E5
			LDIR				; $85E8
			LD A,(HL)				; $85EA
			AND $0F				; $85EB
			LD (DE),A				; $85ED
			INC DE				; $85EE
			XOR (HL)				; $85EF
			RRCA				; $85F0
			RRCA				; $85F1
			RRCA				; $85F2
			RRCA				; $85F3
			LD (DE),A				; $85F4
			LD HL,$85A4				; $85F5
			LD C,(HL)				; $85F8
			LD HL,$85A7				; $85F9
			LD A,($859E)				; $85FC
			LD (HL),A				; $85FF
			INC HL				; $8600
			LD A,($85A0)				; $8601
			LD (HL),A				; $8604
			LD A,($85A5)				; $8605
			LD ($85A9),A				; $8608
			BIT 7,C				; $860B
			EXX				; $860D
			LD HL,$85A7				; $860E
			LD E,L				; $8611
			LD D,H				; $8612
			JR Z,$8618				; $8613
			LD DE,$85A8				; $8615
			EXX				; $8618
			LD B,$01				; $8619
			JR $862E				; $861B

			LD A,(HL)				; $861D
			AND A				; $861E
			JP Z,$870B				; $861F
			LD HL,($85AA)				; $8622
			LD DE,($85AC)				; $8625
			EXX				; $8629
			LD BC,($85AE)				; $862A
			LD HL,($859C)				; $862E
			EXX				; $8631
			LD C,$02				; $8632
			LD A,$18				; $8634
			LD B,(HL)				; $8636
			OUT ($FE),A				; $8637
			EXX				; $8639
			DEC HL				; $863A
			EXX				; $863B
			DJNZ $8639				; $863C
			EX DE,HL				; $863E
			LD A,$00				; $863F
			DEC C				; $8641
			JR NZ,$8636				; $8642
			EXX				; $8644
			BIT 7,H				; $8645
			JR Z,$8631				; $8647
			BIT 7,C				; $8649
			JR Z,$865D				; $864B
			LD HL,$85A7				; $864D
			LD A,($859F)				; $8650
			ADD A,(HL)				; $8653
			LD (HL),A				; $8654
			INC HL				; $8655
			LD A,($85A1)				; $8656
			ADD A,(HL)				; $8659
			LD (HL),A				; $865A
			JR $866F				; $865B

			LD HL,$85A7				; $865D
			LD A,($859F)				; $8660
			BIT 0,B				; $8663
			JR NZ,$866D				; $8665
			LD HL,$85A8				; $8667
			LD A,($85A1)				; $866A
			ADD A,(HL)				; $866D
			LD (HL),A				; $866E
			LD HL,$85A9				; $866F
			DEC (HL)				; $8672
			JP NZ,$86FE				; $8673
			LD HL,$85A3				; $8676
			DEC (HL)				; $8679
			JP Z,$870B				; $867A
			LD HL,$85A2				; $867D
			LD E,(HL)				; $8680
			LD HL,$859F				; $8681
			LD A,(HL)				; $8684
			ADD A,E				; $8685
			LD (HL),A				; $8686
			LD HL,$85A1				; $8687
			LD A,(HL)				; $868A
			ADD A,E				; $868B
			LD (HL),A				; $868C
			BIT 5,C				; $868D
			JR Z,$8699				; $868F
			LD HL,$85A5				; $8691
			DEC (HL)				; $8694
			JR NZ,$8699				; $8695
			LD (HL),$01				; $8697
			BIT 3,C				; $8699
			JR Z,$86AC				; $869B
			BIT 7,C				; $869D
			JR NZ,$86A5				; $869F
			BIT 0,B				; $86A1
			JR Z,$86AC				; $86A3
			LD HL,$859F				; $86A5
			LD A,(HL)				; $86A8
			NEG				; $86A9
			LD (HL),A				; $86AB
			BIT 4,C				; $86AC
			JR Z,$86BF				; $86AE
			BIT 7,C				; $86B0
			JR NZ,$86B8				; $86B2
			BIT 0,B				; $86B4
			JR Z,$86BF				; $86B6
			LD HL,$85A1				; $86B8
			LD A,(HL)				; $86BB
			NEG				; $86BC
			LD (HL),A				; $86BE
			BIT 6,C				; $86BF
			JR Z,$86CF				; $86C1
			LD HL,$85A7				; $86C3
			LD A,($859E)				; $86C6
			LD (HL),A				; $86C9
			INC HL				; $86CA
			LD A,($85A0)				; $86CB
			LD (HL),A				; $86CE
			EXX				; $86CF
			LD HL,$85A7				; $86D0
			LD DE,$85A8				; $86D3
			LD A,($85A5)				; $86D6
			EXX				; $86D9
			BIT 7,C				; $86DA
			JR NZ,$86FB				; $86DC
			LD A,($85A3)				; $86DE
			LD B,C				; $86E1
			SRL A				; $86E2
			JR NC,$86EC				; $86E4
			JR NZ,$86EA				; $86E6
			RR B				; $86E8
			RR B				; $86EA
			BIT 0,B				; $86EC
			EXX				; $86EE
			LD A,($85A5)				; $86EF
			JR NZ,$86F8				; $86F2
			EX DE,HL				; $86F4
			LD A,($85A6)				; $86F5
			LD E,L				; $86F8
			LD D,H				; $86F9
			EXX				; $86FA
			LD ($85A9),A				; $86FB
			LD ($85AE),BC				; $86FE
			EXX				; $8702
			LD ($85AA),HL				; $8703
			LD ($85AC),DE				; $8706
			RET				; $870A

			XOR A				; $870B
			LD ($859A),A				; $870C
			RET				; $870F

			defb $80,$FE,$01                                    ; $8710 ...
			defb $01,$00,$03,$87,$03,$0F,$01,$00				; $8713 ........
			defb $00,$00,$04,$07,$08,$50,$FB,$00				; $871B .....P..
			defb $00,$00,$06,$67,$05,$70,$08,$50				; $8723 ...g.p.P
			defb $FA,$00,$05,$14,$13,$32,$FE,$00				; $872B .....2..
			defb $00,$00                                        ; $8733 ..

			DEC B				; $8735
			RLCA				; $8736
			LD (BC),A				; $8737
			LD HL,$0190				; $8738
			LD ($859C),HL				; $873B
			LD A,$FF				; $873E
			LD ($859B),A				; $8740
			INC A				; $8743
			LD ($859A),A				; $8744
			RET				; $8747

			PUSH AF				; $8748
			PUSH BC				; $8749
			PUSH DE				; $874A
			PUSH HL				; $874B
			PUSH IX				; $874C
			LD HL,$8847				; $874E
			LD C,A				; $8751
			LD A,(HL)				; $8752
			CP $FF				; $8753
			JP Z,$87A5				; $8755
			LD A,C				; $8758
			CP (HL)				; $8759
			JR Z,$8762				; $875A
			LD BC,$0004				; $875C
			ADD HL,BC				; $875F
			JR $8751				; $8760

			LD IX,$8814				; $8762
			BIT 7,(IX+$00)				; $8766
			JR NZ,$87A5				; $876A
			EX AF,AF'				; $876C
			LD A,(IX+$01)				; $876D
			OR A				; $8770
			JR Z,$877B				; $8771
			EX AF,AF'				; $8773
			LD BC,$000A				; $8774
			ADD IX,BC				; $8777
			JR $8766				; $8779

			EX AF,AF'				; $877B
			PUSH HL				; $877C
			CALL $A539				; $877D
			LD (IX+$06),L				; $8780
			LD (IX+$07),H				; $8783
			POP HL				; $8786
			LD (IX+$00),E				; $8787
			LD (IX+$01),D				; $878A
			INC HL				; $878D
			LD A,(HL)				; $878E
			LD (IX+$02),A				; $878F
			LD (IX+$04),A				; $8792
			INC HL				; $8795
			LD A,(HL)				; $8796
			LD (IX+$03),A				; $8797
			LD (IX+$05),A				; $879A
			INC HL				; $879D
			LD A,(HL)				; $879E
			LD (IX+$08),A				; $879F
			LD (IX+$09),A				; $87A2
			POP IX				; $87A5
			POP HL				; $87A7
			POP DE				; $87A8
			POP BC				; $87A9
			POP AF				; $87AA
			RET				; $87AB

			LD IX,$8814				; $87AC
			LD E,(IX+$00)				; $87B0
			BIT 7,E				; $87B3
			RET NZ				; $87B5
			LD A,(IX+$01)				; $87B6
			OR A				; $87B9
			RET Z				; $87BA
			LD D,A				; $87BB
			LD C,E				; $87BC
			CALL $678F				; $87BD
			LD E,C				; $87C0
			LD A,(HL)				; $87C1
			OR A				; $87C2
			JP Z,$880C				; $87C3
			LD D,(IX+$01)				; $87C6
			LD B,D				; $87C9
			LD A,(IX+$09)				; $87CA
			OR A				; $87CD
			JR NZ,$8809				; $87CE
			LD A,(IX+$08)				; $87D0
			LD (IX+$09),A				; $87D3
			LD L,(IX+$04)				; $87D6
			LD H,(IX+$05)				; $87D9
			LD A,(HL)				; $87DC
			CP $FF				; $87DD
			JR NZ,$87EE				; $87DF
			LD L,(IX+$02)				; $87E1
			LD H,(IX+$03)				; $87E4
			LD (IX+$04),L				; $87E7
			LD (IX+$05),H				; $87EA
			LD A,(HL)				; $87ED
			INC HL				; $87EE
			LD (IX+$04),L				; $87EF
			LD (IX+$05),H				; $87F2
			LD L,(IX+$06)				; $87F5
			LD H,(IX+$07)				; $87F8
			CALL $A4DB				; $87FB
			LD (IX+$06),L				; $87FE
			LD (IX+$07),H				; $8801
			LD B,A				; $8804
			CALL $678F				; $8805
			LD (HL),B				; $8808
			DEC (IX+$09)				; $8809
			LD DE,$000A				; $880C
			ADD IX,DE				; $880F
			JP $87B0				; $8811

			defb $FC,$00,$00,$54,$FC,$A8,$00                    ; $8814 ...T...
			defb $54,$FC,$00,$00,$54,$A8,$00,$00				; $881B T...T...
			defb $00,$00,$00,$00,$CF,$CF,$CF,$CF				; $8823 ........
			defb $CF,$CF,$CF,$CF,$0F,$0F,$0F,$0F				; $882B ........
			defb $0F,$0F,$0F,$0F,$4F,$05,$0F,$0F				; $8833 ....O...
			defb $0A,$05,$0F,$0F,$C3,$C3,$C3,$C3				; $883B ........
			defb $C3,$C3,$C3,$FF,$1D,$60,$88,$03				; $8843 .....`..
			defb $20,$64,$88,$03,$1F,$68,$88,$03				; $884B  d...h..
			defb $22,$6C,$88,$03,$52,$70,$88,$05				; $8853 "l..Rp..
			defb $53,$81,$88,$05,$FF,$1D,$1E,$1F				; $885B S.......
			defb $FF,$20,$21,$22,$FF,$1F,$1E,$1D				; $8863 . !"....
			defb $FF,$22,$21,$20,$FF,$52,$52,$52				; $886B ."! .RRR
			defb $52,$52,$52,$54,$56,$58,$58,$58				; $8873 RRRTVXXX
			defb $58,$58,$58,$56,$54,$FF,$53,$53				; $887B XXXVT.SS
			defb $53,$53,$53,$53,$55,$57,$59,$59				; $8883 SSSSUWYY
			defb $59,$59,$59,$59,$57,$55,$FF                    ; $888B YYYYWU.

			LD HL,$88AC				; $8892
			LD E,(HL)				; $8895
			INC HL				; $8896
			LD D,(HL)				; $8897
			LD A,E				; $8898
			OR D				; $8899
			RET Z				; $889A
			INC HL				; $889B
			LD C,(HL)				; $889C
			INC HL				; $889D
			LD B,(HL)				; $889E
			INC HL				; $889F
			PUSH HL				; $88A0
			LD H,D				; $88A1
			LD L,E				; $88A2
			INC DE				; $88A3
			LD (HL),$00				; $88A4
			LDIR				; $88A6
			POP HL				; $88A8
			JP $8895				; $88A9

			defb $57,$7E,$1D,$00,$95,$7D,$37                    ; $88AC W~...}7
			defb $00,$36,$7B,$11,$00,$C4,$6E,$A3				; $88B3 .6{...n.
			defb $01,$14,$88,$31,$00,$08,$7F,$13				; $88BB ...1....
			defb $00,$16,$8B,$59,$00,$6E,$89,$31				; $88C3 ...Y.n.1
			defb $00,$0B,$8C,$8B,$00,$96,$8A,$27				; $88CB .......'
			defb $00,$FC,$6C,$4F,$00,$0B,$8A,$31				; $88D3 ..lO...1
			defb $00,$9F,$97,$4F,$00,$B3,$99,$77				; $88DB ...O...w
			defb $00,$02,$A0,$3B,$00,$00,$00                    ; $88E3 ...;...

			PUSH AF				; $88EA
			PUSH BC				; $88EB
			PUSH HL				; $88EC
			LD HL,$896E				; $88ED
			LD A,(HL)				; $88F0
			CP $FF				; $88F1
			JR Z,$891F				; $88F3
			OR A				; $88F5
			LD BC,$0005				; $88F6
			ADD HL,BC				; $88F9
			JR NZ,$88F0				; $88FA
			SBC HL,BC				; $88FC
			LD (HL),$09				; $88FE
			INC HL				; $8900
			LD (HL),E				; $8901
			INC HL				; $8902
			LD (HL),D				; $8903
			INC HL				; $8904
			LD BC,$C6F9				; $8905
			LD (HL),C				; $8908
			INC HL				; $8909
			LD (HL),B				; $890A
			PUSH DE				; $890B
			PUSH IY				; $890C
			PUSH IX				; $890E
			LD E,$1B				; $8910
			CALL $EF42				; $8912
			LD A,$04				; $8915
			CALL $85B0				; $8917
			POP IX				; $891A
			POP IY				; $891C
			POP DE				; $891E
			POP HL				; $891F
			POP BC				; $8920
			POP AF				; $8921
			RET				; $8922

			LD IX,$896E				; $8923
			LD A,(IX+$00)				; $8927
			CP $FF				; $892A
			RET Z				; $892C
			OR A				; $892D
			JR Z,$895E				; $892E
			LD E,(IX+$01)				; $8930
			LD D,(IX+$02)				; $8933
			LD L,A				; $8936
			LD H,$00				; $8937
			DEC A				; $8939
			LD (IX+$00),A				; $893A
			LD BC,$8964				; $893D
			ADD HL,BC				; $8940
			LD A,(HL)				; $8941
			LD L,(IX+$03)				; $8942
			LD H,(IX+$04)				; $8945
			LD B,D				; $8948
			LD C,E				; $8949
			CALL $A1FD				; $894A
			LD (IX+$03),L				; $894D
			LD (IX+$04),H				; $8950
			LD A,$06				; $8953
			CALL $6679				; $8955
			ADD A,$41				; $8958
			LD C,A				; $895A
			CALL $A446				; $895B
			LD DE,$0005				; $895E
			ADD IX,DE				; $8961
			JR $8927				; $8963

			defb $00,$0C,$0C,$0B,$0B,$0A                        ; $8965 ......
			defb $0A,$09,$09,$2A,$7F,$00,$00,$7B				; $896B ...*...{
			defb $DF,$45,$AA,$3F,$AA,$55,$A2,$15				; $8973 .E.?.U..
			defb $AA,$55,$A2,$15,$AA,$15,$A2,$15				; $897B .U......
			defb $AA,$15,$A2,$15,$2A,$15,$A2,$51				; $8983 ....*..Q
			defb $2A,$15,$A2,$51,$3F,$51,$A2,$F3				; $898B *..Q?Q..
			defb $B7,$00,$00,$F3,$51,$2A,$51,$A2				; $8993 ....Q*Q.
			defb $51,$F3,$F3,$A2,$00,$FF                        ; $899B Q.....

			PUSH AF				; $89A1
			PUSH BC				; $89A2
			PUSH DE				; $89A3
			PUSH HL				; $89A4
			PUSH IX				; $89A5
			LD H,D				; $89A7
			LD L,E				; $89A8
			LD DE,$0005				; $89A9
			LD IX,$8A0B				; $89AC
			EX AF,AF'				; $89B0
			LD A,(IX+$00)				; $89B1
			CP $FF				; $89B4
			JR Z,$89D0				; $89B6
			OR A				; $89B8
			JR Z,$89C0				; $89B9
			ADD IX,DE				; $89BB
			JP $89B1				; $89BD

			EX AF,AF'				; $89C0
			LD (IX+$00),A				; $89C1
			LD (IX+$01),L				; $89C4
			LD (IX+$02),H				; $89C7
			LD (IX+$03),C				; $89CA
			LD (IX+$04),B				; $89CD
			POP IX				; $89D0
			POP HL				; $89D2
			POP DE				; $89D3
			POP BC				; $89D4
			POP AF				; $89D5
			RET				; $89D6

			LD IX,$8A0B				; $89D7
			LD A,(IX+$00)				; $89DB
			CP $FF				; $89DE
			RET Z				; $89E0
			OR A				; $89E1
			JR NZ,$89EC				; $89E2
			LD DE,$0005				; $89E4
			ADD IX,DE				; $89E7
			JP $89DB				; $89E9

			DEC (IX+$00)				; $89EC
			LD E,(IX+$01)				; $89EF
			LD A,(IX+$03)				; $89F2
			CALL $6679				; $89F5
			ADD A,E				; $89F8
			LD E,A				; $89F9
			LD D,(IX+$02)				; $89FA
			LD A,(IX+$04)				; $89FD
			CALL $6679				; $8A00
			ADD A,D				; $8A03
			LD D,A				; $8A04
			CALL $88EA				; $8A05
			JP $89E4				; $8A08

			defb $F3,$F3,$F3,$F3,$00,$00,$00,$00				; $8A0B ........
			defb $80,$80,$80,$80,$00,$00,$00,$00				; $8A13 ........
			defb $F3,$F3,$F3,$F3,$00,$00,$00,$00				; $8A1B ........
			defb $00,$00,$00,$00,$45,$CF,$00,$00				; $8A23 ....E...
			defb $55,$FF,$80,$00,$55,$FF,$80,$00				; $8A2B U...U...
			defb $55,$FF,$D5,$00,$15,$3F,$D5,$AA				; $8A33 U....?..
			defb $15,$3F,$FF                                    ; $8A3B .?.

			PUSH AF				; $8A3E
			PUSH BC				; $8A3F
			PUSH DE				; $8A40
			PUSH HL				; $8A41
			LD HL,$8A96				; $8A42
			LD C,A				; $8A45
			LD A,(HL)				; $8A46
			CP $FF				; $8A47
			JP Z,$8A64				; $8A49
			INC HL				; $8A4C
			INC HL				; $8A4D
			INC HL				; $8A4E
			LD A,(HL)				; $8A4F
			INC HL				; $8A50
			OR A				; $8A51
			JR NZ,$8A46				; $8A52
			DEC HL				; $8A54
			LD (HL),$28				; $8A55
			DEC HL				; $8A57
			LD (HL),C				; $8A58
			DEC HL				; $8A59
			LD (HL),D				; $8A5A
			DEC HL				; $8A5B
			LD (HL),E				; $8A5C
			LD A,C				; $8A5D
			LD HL,$C6F9				; $8A5E
			CALL $A4DB				; $8A61
			POP HL				; $8A64
			POP DE				; $8A65
			POP BC				; $8A66
			POP AF				; $8A67
			RET				; $8A68

			LD HL,$8A96				; $8A69
			LD A,(HL)				; $8A6C
			CP $FF				; $8A6D
			RET Z				; $8A6F
			LD E,A				; $8A70
			INC HL				; $8A71
			LD D,(HL)				; $8A72
			INC HL				; $8A73
			LD C,(HL)				; $8A74
			INC HL				; $8A75
			LD A,(HL)				; $8A76
			INC HL				; $8A77
			OR A				; $8A78
			JP Z,$8A6C				; $8A79
			DEC HL				; $8A7C
			DEC (HL)				; $8A7D
			INC HL				; $8A7E
			LD A,C				; $8A7F
			PUSH HL				; $8A80
			LD HL,$C6F9				; $8A81
			CALL Z,$A4DB				; $8A84
			POP HL				; $8A87
			LD A,($65EA)				; $8A88
			AND $07				; $8A8B
			OR $40				; $8A8D
			LD C,A				; $8A8F
			CALL $A4AD				; $8A90
			JP $8A6C				; $8A93

			defb $00,$00,$45,$22,$00                            ; $8A93 ..E".
			defb $00,$45,$22,$00,$00,$45,$22,$00				; $8A9B .E"..E".
			defb $CF,$CF,$9B,$33,$00,$00,$00,$00				; $8AA3 ...3....
			defb $45,$CF,$33,$22,$45,$CF,$33,$22				; $8AAB E.3"E.3"
			defb $00,$00,$00,$00,$00,$CF,$33,$00				; $8AB3 ......3.
			defb $00,$CF,$33,$FF                                ; $8ABB ..3.

			BIT 7,E				; $8ABF
			RET NZ				; $8AC1
			PUSH HL				; $8AC2
			LD HL,$8B16				; $8AC3
			BIT 7,(HL)				; $8AC6
			JR NZ,$8AD8				; $8AC8
			INC HL				; $8ACA
			INC HL				; $8ACB
			LD A,(HL)				; $8ACC
			OR A				; $8ACD
			INC HL				; $8ACE
			JR NZ,$8AC6				; $8ACF
			DEC HL				; $8AD1
			LD (HL),$0A				; $8AD2
			DEC HL				; $8AD4
			LD (HL),D				; $8AD5
			DEC HL				; $8AD6
			LD (HL),E				; $8AD7
			POP HL				; $8AD8
			RET				; $8AD9

			LD HL,$8B16				; $8ADA
			LD E,(HL)				; $8ADD
			BIT 7,E				; $8ADE
			RET NZ				; $8AE0
			INC HL				; $8AE1
			LD D,(HL)				; $8AE2
			INC HL				; $8AE3
			LD A,(HL)				; $8AE4
			INC HL				; $8AE5
			OR A				; $8AE6
			JR Z,$8ADD				; $8AE7
			PUSH HL				; $8AE9
			DEC HL				; $8AEA
			DEC (HL)				; $8AEB
			LD L,A				; $8AEC
			LD H,$00				; $8AED
			LD BC,$8B0A				; $8AEF
			ADD HL,BC				; $8AF2
			INC HL				; $8AF3
			LD A,(HL)				; $8AF4
			CALL $80BD				; $8AF5
			DEC HL				; $8AF8
			LD A,(HL)				; $8AF9
			CALL $80BD				; $8AFA
			LD A,$05				; $8AFD
			CALL $6679				; $8AFF
			ADD A,$42				; $8B02
			LD C,A				; $8B04
			CALL $A4AD				; $8B05
			POP HL				; $8B08
			JR $8ADD				; $8B09

			defb $09,$08,$08,$08,$03,$08,$06,$08				; $8B0B ........
			defb $07,$06,$09,$00,$00,$45,$22,$00				; $8B13 .....E".
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $8B1B ........
			defb $00,$45,$22,$00,$00,$45,$22,$00				; $8B23 .E"..E".
			defb $00,$45,$22,$00,$00,$00,$00,$00				; $8B2B .E".....
			defb $00,$04,$80,$00,$00,$04,$80,$00				; $8B33 ........
			defb $00,$04,$80,$00,$00,$00,$00,$00				; $8B3B ........
			defb $00,$CF,$33,$00,$00,$CF,$33,$00				; $8B43 ..3...3.
			defb $00,$CF,$33,$00,$00,$00,$00,$00				; $8B4B ..3.....
			defb $45,$CF,$33,$22,$45,$CF,$33,$22				; $8B53 E.3"E.3"
			defb $00,$00,$00,$00,$CF,$CF,$9B,$33				; $8B5B .......3
			defb $00,$00,$00,$00,$00,$45,$22,$00				; $8B63 .....E".
			defb $00,$45,$22,$00,$00,$FF                        ; $8B6B .E"...

			PUSH AF				; $8B71
			PUSH BC				; $8B72
			PUSH DE				; $8B73
			PUSH HL				; $8B74
			LD H,D				; $8B75
			LD L,E				; $8B76
			AND A				; $8B77
			SBC HL,BC				; $8B78
			JR Z,$8BC5				; $8B7A
			LD H,E				; $8B7C
			LD L,D				; $8B7D
			LD A,B				; $8B7E
			LD B,C				; $8B7F
			LD C,A				; $8B80
			LD DE,$0101				; $8B81
			LD A,B				; $8B84
			SUB H				; $8B85
			JR NC,$8B8C				; $8B86
			LD D,$FF				; $8B88
			NEG				; $8B8A
			LD B,A				; $8B8C
			LD A,C				; $8B8D
			SUB L				; $8B8E
			JR NC,$8B95				; $8B8F
			LD E,$FF				; $8B91
			NEG				; $8B93
			LD C,A				; $8B95
			OR B				; $8B96
			RET Z				; $8B97
			LD A,C				; $8B98
			CP B				; $8B99
			LD (IX+$00),H				; $8B9A
			LD (IX+$01),L				; $8B9D
			LD H,D				; $8BA0
			LD L,E				; $8BA1
			LD (IX+$04),H				; $8BA2
			LD (IX+$03),L				; $8BA5
			LD L,$00				; $8BA8
			JR C,$8BB0				; $8BAA
			LD H,L				; $8BAC
			LD L,E				; $8BAD
			LD C,B				; $8BAE
			LD B,A				; $8BAF
			LD (IX+$06),H				; $8BB0
			LD (IX+$05),L				; $8BB3
			LD (IX+$02),B				; $8BB6
			LD A,B				; $8BB9
			SRL A				; $8BBA
			LD (IX+$07),A				; $8BBC
			LD (IX+$09),B				; $8BBF
			LD (IX+$08),C				; $8BC2
			POP HL				; $8BC5
			POP DE				; $8BC6
			POP BC				; $8BC7
			POP AF				; $8BC8
			RET				; $8BC9

			PUSH BC				; $8BCA
			PUSH HL				; $8BCB
			LD B,(IX+$09)				; $8BCC
			LD C,(IX+$08)				; $8BCF
			LD L,(IX+$07)				; $8BD2
			LD A,L				; $8BD5
			ADD A,C				; $8BD6
			JR C,$8BDC				; $8BD7
			CP B				; $8BD9
			JR C,$8BE8				; $8BDA
			SUB B				; $8BDC
			LD (IX+$07),A				; $8BDD
			LD D,(IX+$04)				; $8BE0
			LD E,(IX+$03)				; $8BE3
			JR $8BF1				; $8BE6

			LD (IX+$07),A				; $8BE8
			LD D,(IX+$06)				; $8BEB
			LD E,(IX+$05)				; $8BEE
			LD H,(IX+$00)				; $8BF1
			LD L,(IX+$01)				; $8BF4
			LD A,H				; $8BF7
			ADD A,D				; $8BF8
			LD H,A				; $8BF9
			LD A,L				; $8BFA
			ADD A,E				; $8BFB
			LD L,A				; $8BFC
			LD D,L				; $8BFD
			LD E,H				; $8BFE
			DEC (IX+$02)				; $8BFF
			LD (IX+$00),E				; $8C02
			LD (IX+$01),D				; $8C05
			POP HL				; $8C08
			POP BC				; $8C09
			RET				; $8C0A

			defb $A0,$DA,$00,$50,$A0,$45,$00,$50				; $8C0B ...P.E.P
			defb $A0,$45,$A0,$B0,$00,$00,$8A,$A0				; $8C13 .E......
			defb $00,$00,$DA,$20,$00,$00,$14,$20				; $8C1B ... ... 
			defb $00,$00,$00,$00,$00,$00,$00,$10				; $8C23 ........
			defb $00,$00,$00,$50,$00,$A0,$00,$B0				; $8C2B ...P....
			defb $00,$F0,$F0,$20,$10,$50,$20,$00				; $8C33 ... .P .
			defb $10,$10,$00,$00,$50,$20,$00,$00				; $8C3B ....P ..
			defb $50,$A0,$00,$00,$50,$B0,$00,$00				; $8C43 P...P...
			defb $10,$20,$00,$00,$00,$50,$00,$00				; $8C4B . ...P..
			defb $50,$00,$A0,$00,$10,$20,$A0,$00				; $8C53 P.... ..
			defb $00,$A0,$A0,$00,$00,$A0,$A0,$00				; $8C5B ........
			defb $00,$00,$9E,$20,$00,$00,$9E,$00				; $8C63 ... ....
			defb $00,$00,$9E,$00,$00,$00,$DA,$00				; $8C6B ........
			defb $00,$45,$DA,$00,$00,$45,$78,$00				; $8C73 .E...Ex.
			defb $00,$45,$78,$20,$00,$45,$78,$45				; $8C7B .Ex .ExE
			defb $00,$9E,$78,$45,$00,$9E,$F0,$45				; $8C83 ..xE...E
			defb $00,$9E,$F0,$45,$45,$78,$F0,$20				; $8C8B ...EEx. 
			defb $45,$78,$F0,$20,$FF                            ; $8C93 Ex. .

			PUSH AF				; $8C98
			PUSH BC				; $8C99
			PUSH DE				; $8C9A
			PUSH HL				; $8C9B
			PUSH IX				; $8C9C
			EX AF,AF'				; $8C9E
			LD IX,$8C0B				; $8C9F
			LD A,(IX+$00)				; $8CA3
			CP $FF				; $8CA6
			JR Z,$8CD0				; $8CA8
			LD A,(IX+$02)				; $8CAA
			OR A				; $8CAD
			JR Z,$8CBA				; $8CAE
			PUSH DE				; $8CB0
			LD DE,$000E				; $8CB1
			ADD IX,DE				; $8CB4
			POP DE				; $8CB6
			JP $8CA3				; $8CB7

			EX AF,AF'				; $8CBA
			LD (IX+$0B),L				; $8CBB
			LD (IX+$0A),A				; $8CBE
			LD (IX+$0C),H				; $8CC1
			CALL $80BD				; $8CC4
			CALL $8B71				; $8CC7
			LD A,(IX+$02)				; $8CCA
			LD (IX+$0D),A				; $8CCD
			POP IX				; $8CD0
			POP HL				; $8CD2
			POP DE				; $8CD3
			POP BC				; $8CD4
			POP AF				; $8CD5
			RET				; $8CD6

			LD IX,$8C0B				; $8CD7
			LD A,(IX+$00)				; $8CDB
			CP $FF				; $8CDE
			RET Z				; $8CE0
			LD A,(IX+$02)				; $8CE1
			OR A				; $8CE4
			JR NZ,$8CEF				; $8CE5
			LD DE,$000E				; $8CE7
			ADD IX,DE				; $8CEA
			JP $8CDB				; $8CEC

			LD E,(IX+$00)				; $8CEF
			LD D,(IX+$01)				; $8CF2
			LD A,(IX+$0A)				; $8CF5
			CALL $80BD				; $8CF8
			LD B,(IX+$0B)				; $8CFB
			CALL $8BCA				; $8CFE
			JR Z,$8D32				; $8D01
			DJNZ $8CFE				; $8D03
			LD A,(IX+$0D)				; $8D05
			SUB (IX+$02)				; $8D08
			CP $14				; $8D0B
			JR C,$8D16				; $8D0D
			CALL $678F				; $8D0F
			LD A,(HL)				; $8D12
			OR A				; $8D13
			JR NZ,$8D32				; $8D14
			LD A,(IX+$0A)				; $8D16
			CALL $80BD				; $8D19
			PUSH DE				; $8D1C
			INC D				; $8D1D
			INC D				; $8D1E
			INC D				; $8D1F
			INC D				; $8D20
			INC E				; $8D21
			INC E				; $8D22
			LD A,$03				; $8D23
			CALL $8E31				; $8D25
			POP DE				; $8D28
			LD C,(IX+$0C)				; $8D29
			CALL $A4AD				; $8D2C
			JP $8CE7				; $8D2F

			LD (IX+$02),$00				; $8D32
			CALL $8ABF				; $8D36
			JP $8CE7				; $8D39

			LD HL,$8E1C				; $8D3C
			LD A,(HL)				; $8D3F
			CP $FF				; $8D40
			RET Z				; $8D42
			LD E,A				; $8D43
			INC HL				; $8D44
			LD D,(HL)				; $8D45
			INC HL				; $8D46
			PUSH HL				; $8D47
			CALL $678F				; $8D48
			LD A,(HL)				; $8D4B
			OR A				; $8D4C
			JR NZ,$8D53				; $8D4D
			POP HL				; $8D4F
			JP $8D3F				; $8D50

			LD ($8D7A),A				; $8D53
			LD HL,$8D7B				; $8D56
			LD C,A				; $8D59
			LD A,(HL)				; $8D5A
			CP $FF				; $8D5B
			JP Z,$8D4F				; $8D5D
			CP C				; $8D60
			JR Z,$8D6C				; $8D61
			PUSH BC				; $8D63
			LD BC,$0005				; $8D64
			ADD HL,BC				; $8D67
			POP BC				; $8D68
			JP $8D5A				; $8D69

			INC HL				; $8D6C
			LD A,(HL)				; $8D6D
			ADD A,E				; $8D6E
			LD E,A				; $8D6F
			INC HL				; $8D70
			LD A,(HL)				; $8D71
			ADD A,D				; $8D72
			LD D,A				; $8D73
			INC HL				; $8D74
			LD A,(HL)				; $8D75
			INC HL				; $8D76
			LD H,(HL)				; $8D77
			LD L,A				; $8D78
			JP (HL)				; $8D79

			defb $00                                            ; $8D7A .
			defb $27,$00,$00,$95,$8D,$32,$00,$08				; $8D7B '....2..
			defb $95,$8D,$58,$06,$05,$C8,$8D,$94				; $8D83 ..X.....
			defb $FF,$04,$F7,$8D,$98,$07,$04,$0F				; $8D8B ........
			defb $8E,$FF                                        ; $8D93 ..

			LD A,$0C				; $8D95
			CALL $6679				; $8D97
			OR A				; $8D9A
			JP NZ,$8D4F				; $8D9B
			LD BC,($6969)				; $8D9E
			LD A,$1F				; $8DA2
			CALL $6679				; $8DA4
			ADD A,C				; $8DA7
			SUB $0C				; $8DA8
			CP $7C				; $8DAA
			JR NC,$8DAF				; $8DAC
			LD C,A				; $8DAE
			LD A,$3F				; $8DAF
			CALL $6679				; $8DB1
			ADD A,B				; $8DB4
			SUB $18				; $8DB5
			LD B,A				; $8DB7
			LD A,($74E1)				; $8DB8
			ADD A,$02				; $8DBB
			LD L,A				; $8DBD
			LD H,$44				; $8DBE
			LD A,$0A				; $8DC0
			CALL $8C98				; $8DC2
			JP $8D4F				; $8DC5

			LD A,$06				; $8DC8
			CALL $6679				; $8DCA
			OR A				; $8DCD
			JP NZ,$8D4F				; $8DCE
			LD BC,($6969)				; $8DD1
			LD A,$1F				; $8DD5
			CALL $6679				; $8DD7
			ADD A,C				; $8DDA
			SUB $0C				; $8DDB
			CP $7C				; $8DDD
			JR NC,$8DE2				; $8DDF
			LD C,A				; $8DE1
			LD A,$3F				; $8DE2
			CALL $6679				; $8DE4
			ADD A,B				; $8DE7
			SUB $18				; $8DE8
			LD B,A				; $8DEA
			LD L,$02				; $8DEB
			LD H,$42				; $8DED
			LD A,$05				; $8DEF
			CALL $8C98				; $8DF1
			JP $8D4F				; $8DF4

			LD A,$16				; $8DF7
			CALL $6679				; $8DF9
			OR A				; $8DFC
			JP NZ,$8D4F				; $8DFD
			LD C,$00				; $8E00
			LD B,D				; $8E02
			LD H,$42				; $8E03
			LD L,$02				; $8E05
			LD A,$05				; $8E07
			CALL $8C98				; $8E09
			JP $8D4F				; $8E0C

			LD A,$16				; $8E0F
			CALL $6679				; $8E11
			OR A				; $8E14
			JP NZ,$8D4F				; $8E15
			LD C,$7B				; $8E18
			JR $8E02				; $8E1A

			defb $B0,$30,$20,$00,$30,$30,$00                    ; $8E1C .0 .00.
			defb $FC,$22,$03,$02,$FC,$22,$03,$03				; $8E23 ."..."..
			defb $FC,$33,$54,$A9,$FC,$FF,$E5,$2A				; $8E2B .3T....*
			defb $42,$8E,$77,$23,$73,$23,$72,$23				; $8E33 B.w#s#r#
			defb $36,$FF,$22,$42,$8E,$E1,$C9,$44				; $8E3B 6."B...D
			defb $8E,$56,$B9,$22,$00,$00,$00,$00				; $8E43 .V."....
			defb $45,$03,$FC,$33,$45,$8A,$ED,$11				; $8E4B E..3E...
			defb $45,$00,$A8,$11,$45,$03,$FC,$33				; $8E53 E...E..3
			defb $45,$03,$FC,$33,$45,$8A,$ED,$11				; $8E5B E..3E...
			defb $45,$00,$A8,$11,$45,$03,$FC,$33				; $8E63 E...E..3
			defb $00,$00,$00,$00,$45,$3C,$3C,$30				; $8E6B ....E<<0
			defb $45,$3C,$78,$30,$45,$3C,$F0,$30				; $8E73 E<x0E<.0
			defb $45,$3C,$B0,$30,$45,$3C,$B0,$30				; $8E7B E<.0E<.0
			defb $00,$9E,$B0,$20,$00,$9E,$30,$20				; $8E83 ... ..0 
			defb $00,$45,$30,$00,$00,$00,$00,$00				; $8E8B .E0.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $8E93 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $8E9B ........
			defb $9E,$78,$F0,$A0,$28,$00,$00,$20				; $8EA3 .x..(.. 
			defb $28,$78,$20,$20,$28,$B0,$00,$20				; $8EAB (x  (.. 
			defb $28,$A0,$00,$20,$28,$00,$00,$20				; $8EB3 (.. (.. 
			defb $A0,$78,$20,$20,$A0,$B0,$00,$20				; $8EBB .x  ... 
			defb $A0,$A0,$00,$20,$A0,$00,$00,$20				; $8EC3 ... ... 
			defb $A0,$78,$20,$20,$A0,$B0,$00,$20				; $8ECB .x  ... 
			defb $A0,$A0,$00,$20,$A0,$00,$00,$FF				; $8ED3 ... ....

			LD A,($8F94)				; $8EDB
			OR A				; $8EDE
			JR NZ,$8F58				; $8EDF
			LD DE,($6969)				; $8EE1
			LD HL,($71B2)				; $8EE5
			DEC HL				; $8EE8
			LD A,H				; $8EE9
			OR L				; $8EEA
			JP Z,$8F0C				; $8EEB
			LD ($71B2),HL				; $8EEE
			LD A,($7E94)				; $8EF1
			OR A				; $8EF4
			RET NZ				; $8EF5
			LD HL,$8E44				; $8EF6
			LD A,(HL)				; $8EF9
			CP $FF				; $8EFA
			RET Z				; $8EFC
			CALL $67B9				; $8EFD
			INC HL				; $8F00
			LD C,(HL)				; $8F01
			INC HL				; $8F02
			LD B,(HL)				; $8F03
			CALL $6802				; $8F04
			INC HL				; $8F07
			OR A				; $8F08
			JP Z,$8EF9				; $8F09
			LD A,$64				; $8F0C
			LD ($8F94),A				; $8F0E
			LD A,E				; $8F11
			SUB $08				; $8F12
			LD E,A				; $8F14
			LD A,D				; $8F15
			SUB $10				; $8F16
			LD D,A				; $8F18
			LD BC,$2010				; $8F19
			LD A,$14				; $8F1C
			CALL $89A1				; $8F1E
			CALL $89A1				; $8F21
			LD DE,($6969)				; $8F24
			LD B,D				; $8F28
			LD C,E				; $8F29
			LD HL,$C6F9				; $8F2A
			LD ($696D),HL				; $8F2D
			LD ($6AEF),HL				; $8F30
			LD ($6AF4),HL				; $8F33
			CALL $6953				; $8F36
			LD HL,$C6F9				; $8F39
			LD ($696D),HL				; $8F3C
			LD ($6AEF),HL				; $8F3F
			LD ($6AF4),HL				; $8F42
			XOR A				; $8F45
			LD ($6AEE),A				; $8F46
			LD ($6AF3),A				; $8F49
			LD HL,$78D3				; $8F4C
			DEC (HL)				; $8F4F
			LD E,$20				; $8F50
			CALL $EF42				; $8F52
			JP $78C8				; $8F55

			DEC A				; $8F58
			LD ($8F94),A				; $8F59
			RET NZ				; $8F5C
			LD A,($78D3)				; $8F5D
			OR A				; $8F60
			JP Z,$8F95				; $8F61
			LD HL,$0753				; $8F64
			LD ($71B2),HL				; $8F67
			LD A,$32				; $8F6A
			LD ($7E94),A				; $8F6C
			CALL $7BFC				; $8F6F
			CALL $7B72				; $8F72
			LD DE,($6970)				; $8F75
			LD ($6969),DE				; $8F79
			LD B,D				; $8F7D
			LD C,E				; $8F7E
			JP $6953				; $8F7F

			defb $E6                                            ; $8F82 .
			defb $F1,$C2,$DF,$0A,$0A,$E0,$46,$47				; $8F83 ......FG
			defb $45,$54,$20,$52,$45,$41,$44,$59				; $8F8B ET READY
			defb $FF,$00                                        ; $8F93 ..

			LD E,$22				; $8F95
			CALL $EF42				; $8F97
			LD HL,$C2F1				; $8F9A
			LD ($6C55),HL				; $8F9D
			LD HL,$8FCD				; $8FA0
			LD BC,$0945				; $8FA3
			LD DE,$0E0B				; $8FA6
			PUSH BC				; $8FA9
			LD A,(HL)				; $8FAA
			CALL $6C4A				; $8FAB
			INC HL				; $8FAE
			INC E				; $8FAF
			LD BC,$2710				; $8FB0
			CALL $676A				; $8FB3
			POP BC				; $8FB6
			DJNZ $8FA9				; $8FB7
			CALL $671E				; $8FB9
			LD BC,$0000				; $8FBC
			LD A,$07				; $8FBF
			CALL $676A				; $8FC1
			DEC A				; $8FC4
			JR NZ,$8FC1				; $8FC5
			CALL $954F				; $8FC7
			JP $9433				; $8FCA

			defb $47,$41,$4D,$45,$20,$4F                        ; $8FCD GAME O
			defb $56,$45,$52                                    ; $8FD3 VER

			CP $F0				; $8FD6
			RET C				; $8FD8
			CP $F8				; $8FD9
			RET NC				; $8FDB
			PUSH AF				; $8FDC
			PUSH BC				; $8FDD
			PUSH DE				; $8FDE
			PUSH HL				; $8FDF
			PUSH IX				; $8FE0
			LD IX,($9140)				; $8FE2
			LD HL,$C6F9				; $8FE6
			LD (IX+$03),L				; $8FE9
			LD (IX+$04),H				; $8FEC
			LD (IX+$08),L				; $8FEF
			LD (IX+$09),H				; $8FF2
			LD (IX+$00),E				; $8FF5
			LD (IX+$01),D				; $8FF8
			LD (IX+$02),$3E				; $8FFB
			SUB $F0				; $8FFF
			ADD A,A				; $9001
			LD L,A				; $9002
			LD H,$00				; $9003
			LD BC,$9030				; $9005
			ADD HL,BC				; $9008
			LD A,(HL)				; $9009
			ADD A,D				; $900A
			LD D,A				; $900B
			INC HL				; $900C
			LD A,(HL)				; $900D
			LD (IX+$0A),A				; $900E
			LD (IX+$05),E				; $9011
			LD (IX+$06),D				; $9014
			LD A,$42				; $9017
			LD (IX+$07),A				; $9019
			LD DE,$000B				; $901C
			ADD IX,DE				; $901F
			LD (IX+$00),$FF				; $9021
			LD ($9140),IX				; $9025
			POP IX				; $9029
			POP HL				; $902B
			POP DE				; $902C
			POP BC				; $902D
			POP AF				; $902E
			RET				; $902F

			defb $3C,$FF,$37                                    ; $9030 <.7
			defb $FF,$32,$FF,$2D,$FF,$3C,$01,$37				; $9033 .2.-.<.7
			defb $01,$32,$01,$2D,$01                            ; $903B .2.-.

			LD IX,$9142				; $9040
			LD A,(IX+$00)				; $9044
			CP $FF				; $9047
			RET Z				; $9049
			LD A,(IX+$0A)				; $904A
			CP $FF				; $904D
			JP Z,$905D				; $904F
			JP $90CA				; $9052

			LD DE,$000B				; $9055
			ADD IX,DE				; $9058
			JP $9044				; $905A

			LD E,(IX+$00)				; $905D
			LD D,(IX+$01)				; $9060
			LD B,D				; $9063
			LD C,E				; $9064
			CALL $69E6				; $9065
			CALL NZ,$9137				; $9068
			LD A,(IX+$0A)				; $906B
			ADD A,D				; $906E
			LD D,A				; $906F
			LD (IX+$01),D				; $9070
			LD A,($65EA)				; $9073
			AND $03				; $9076
			ADD A,(IX+$02)				; $9078
			LD L,(IX+$03)				; $907B
			LD H,(IX+$04)				; $907E
			CALL $A4DB				; $9081
			LD (IX+$03),L				; $9084
			LD (IX+$04),H				; $9087
			LD A,$01				; $908A
			CALL $8E31				; $908C
			LD BC,$0747				; $908F
			CALL $A47B				; $9092
			LD E,(IX+$05)				; $9095
			LD D,(IX+$06)				; $9098
			LD B,D				; $909B
			LD C,E				; $909C
			LD A,(IX+$0A)				; $909D
			ADD A,D				; $90A0
			LD D,A				; $90A1
			LD (IX+$06),D				; $90A2
			LD A,($65EA)				; $90A5
			AND $03				; $90A8
			ADD A,(IX+$07)				; $90AA
			LD L,(IX+$08)				; $90AD
			LD H,(IX+$09)				; $90B0
			CALL $A4DB				; $90B3
			LD (IX+$08),L				; $90B6
			LD (IX+$09),H				; $90B9
			LD A,$01				; $90BC
			CALL $8E31				; $90BE
			LD BC,$0747				; $90C1
			CALL $A47B				; $90C4
			JP $9055				; $90C7

			LD E,(IX+$05)				; $90CA
			LD D,(IX+$06)				; $90CD
			LD B,D				; $90D0
			LD C,E				; $90D1
			CALL $69C4				; $90D2
			CALL NZ,$9137				; $90D5
			LD A,(IX+$0A)				; $90D8
			ADD A,D				; $90DB
			LD D,A				; $90DC
			LD (IX+$06),D				; $90DD
			LD A,($65EA)				; $90E0
			AND $03				; $90E3
			ADD A,(IX+$07)				; $90E5
			LD L,(IX+$08)				; $90E8
			LD H,(IX+$09)				; $90EB
			CALL $A4DB				; $90EE
			LD (IX+$08),L				; $90F1
			LD (IX+$09),H				; $90F4
			LD A,$01				; $90F7
			CALL $8E31				; $90F9
			LD BC,$0747				; $90FC
			CALL $A47B				; $90FF
			LD E,(IX+$00)				; $9102
			LD D,(IX+$01)				; $9105
			LD B,D				; $9108
			LD C,E				; $9109
			LD A,(IX+$0A)				; $910A
			ADD A,D				; $910D
			LD D,A				; $910E
			LD (IX+$01),D				; $910F
			LD A,($65EA)				; $9112
			AND $03				; $9115
			ADD A,(IX+$02)				; $9117
			LD L,(IX+$03)				; $911A
			LD H,(IX+$04)				; $911D
			CALL $A4DB				; $9120
			LD (IX+$03),L				; $9123
			LD (IX+$04),H				; $9126
			LD A,$01				; $9129
			CALL $8E31				; $912B
			LD BC,$0747				; $912E
			CALL $A47B				; $9131
			JP $9055				; $9134

			LD A,(IX+$0A)				; $9137
			NEG				; $913A
			LD (IX+$0A),A				; $913C
			RET				; $913F

			defb $42,$91,$00                                    ; $9140 B..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9143 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $914B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9153 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $915B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9163 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $916B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9173 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $917B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9183 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $918B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $9193 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $919B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $91A3 ........
			defb $00,$00,$00,$00,$00,$FF                        ; $91AB ......

			CP $F8				; $91B1
			RET C				; $91B3
			PUSH AF				; $91B4
			PUSH BC				; $91B5
			PUSH DE				; $91B6
			PUSH HL				; $91B7
			PUSH IX				; $91B8
			LD IX,($9265)				; $91BA
			LD HL,$C6F9				; $91BE
			LD (IX+$05),L				; $91C1
			LD (IX+$06),H				; $91C4
			LD HL,$91FE				; $91C7
			SUB $F8				; $91CA
			ADD A,A				; $91CC
			ADD A,A				; $91CD
			LD L,A				; $91CE
			LD H,$00				; $91CF
			LD BC,$91FE				; $91D1
			ADD HL,BC				; $91D4
			LD A,(HL)				; $91D5
			ADD A,E				; $91D6
			LD (IX+$00),A				; $91D7
			INC HL				; $91DA
			LD A,(HL)				; $91DB
			ADD A,D				; $91DC
			LD (IX+$01),A				; $91DD
			INC HL				; $91E0
			LD A,(HL)				; $91E1
			LD (IX+$02),A				; $91E2
			INC HL				; $91E5
			LD A,(HL)				; $91E6
			LD (IX+$03),A				; $91E7
			LD DE,$0007				; $91EA
			ADD IX,DE				; $91ED
			LD (IX+$00),$FF				; $91EF
			LD ($9265),IX				; $91F3
			POP IX				; $91F7
			POP HL				; $91F9
			POP DE				; $91FA
			POP BC				; $91FB
			POP AF				; $91FC
			RET				; $91FD

			defb $00,$04,$01,$01,$02                            ; $91FE .....
			defb $00,$02,$01,$00,$FC,$03,$01,$FE				; $9203 ........
			defb $00,$04,$01,$00,$04,$01,$02,$02				; $920B ........
			defb $00,$02,$02,$00,$FC,$03,$02,$FE				; $9213 ........
			defb $00,$04,$02,$20,$45,$78,$B0,$20				; $921B ... Ex. 
			defb $8A,$00,$00,$10,$CF,$3C,$F0,$30				; $9223 .....<.0
			defb $CF,$3C,$F0,$30,$00,$00,$00,$00				; $922B .<.0....
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $9233 Ex. Ex. 
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $923B Ex. Ex. 
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $9243 Ex. Ex. 
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $924B Ex. Ex. 
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $9253 Ex. Ex. 
			defb $45,$78,$B0,$20,$45,$78,$B0,$20				; $925B Ex. Ex. 
			defb $45,$FF,$00,$00                                ; $9263 E...

			LD IX,$921E				; $9267
			LD A,(IX+$00)				; $926B
			CP $FF				; $926E
			RET Z				; $9270
			LD A,(IX+$02)				; $9271
			OR A				; $9274
			JR NZ,$927F				; $9275
			LD BC,$0007				; $9277
			ADD IX,BC				; $927A
			JP $926B				; $927C

			LD E,(IX+$00)				; $927F
			LD D,(IX+$01)				; $9282
			LD A,(IX+$04)				; $9285
			OR A				; $9288
			JP NZ,$9376				; $9289
			LD A,(IX+$02)				; $928C
			CP $01				; $928F
			JR Z,$92B2				; $9291
			CP $02				; $9293
			JR Z,$92C9				; $9295
			CP $03				; $9297
			JR Z,$92E0				; $9299
			CALL $9421				; $929B
			JR Z,$92A5				; $929E
			LD A,$03				; $92A0
			JP $92F7				; $92A2

			CALL $93FC				; $92A5
			LD A,$00				; $92A8
			JP NZ,$92F7				; $92AA
			LD A,$01				; $92AD
			JP $92F7				; $92AF

			CALL $93FC				; $92B2
			JR Z,$92BC				; $92B5
			LD A,$04				; $92B7
			JP $92F7				; $92B9

			CALL $940F				; $92BC
			LD A,$00				; $92BF
			JP NZ,$92F7				; $92C1
			LD A,$02				; $92C4
			JP $92F7				; $92C6

			CALL $940F				; $92C9
			JR Z,$92D3				; $92CC
			LD A,$01				; $92CE
			JP $92F7				; $92D0

			CALL $93EA				; $92D3
			LD A,$00				; $92D6
			JP NZ,$92F7				; $92D8
			LD A,$03				; $92DB
			JP $92F7				; $92DD

			CALL $93EA				; $92E0
			JR Z,$92EA				; $92E3
			LD A,$02				; $92E5
			JP $92F7				; $92E7

			CALL $9421				; $92EA
			LD A,$00				; $92ED
			JP NZ,$92F7				; $92EF
			LD A,$04				; $92F2
			JP $92F7				; $92F4

			LD H,A				; $92F7
			LD L,(IX+$02)				; $92F8
			LD B,(IX+$03)				; $92FB
			OR A				; $92FE
			JR Z,$9304				; $92FF
			LD (IX+$02),A				; $9301
			LD A,(IX+$02)				; $9304
			CP $01				; $9307
			JR NZ,$9312				; $9309
			LD A,B				; $930B
			NEG				; $930C
			ADD A,E				; $930E
			LD E,A				; $930F
			JR $932B				; $9310

			CP $02				; $9312
			JR NZ,$931C				; $9314
			LD A,B				; $9316
			ADD A,A				; $9317
			ADD A,D				; $9318
			LD D,A				; $9319
			JR $932B				; $931A

			CP $03				; $931C
			JR NZ,$9325				; $931E
			LD A,B				; $9320
			ADD A,E				; $9321
			LD E,A				; $9322
			JR $932B				; $9323

			LD A,B				; $9325
			NEG				; $9326
			ADD A,A				; $9328
			ADD A,D				; $9329
			LD D,A				; $932A
			LD A,(IX+$04)				; $932B
			OR A				; $932E
			JR NZ,$9349				; $932F
			LD A,H				; $9331
			OR A				; $9332
			JR Z,$9349				; $9333
			CALL $93D0				; $9335
			JR NZ,$9349				; $9338
			LD (IX+$04),$01				; $933A
			LD (IX+$02),L				; $933E
			LD E,(IX+$00)				; $9341
			LD D,(IX+$01)				; $9344
			JR $9376				; $9347

			LD C,(IX+$00)				; $9349
			LD B,(IX+$01)				; $934C
			LD (IX+$00),E				; $934F
			LD (IX+$01),D				; $9352
			LD L,(IX+$05)				; $9355
			LD H,(IX+$06)				; $9358
			LD A,(IX+$02)				; $935B
			ADD A,$02				; $935E
			CALL $A1FD				; $9360
			LD A,$01				; $9363
			CALL $8E31				; $9365
			LD (IX+$05),L				; $9368
			LD (IX+$06),H				; $936B
			LD C,$47				; $936E
			CALL $A446				; $9370
			JP $9277				; $9373

			LD A,(IX+$02)				; $9376
			CP $01				; $9379
			JP Z,$939A				; $937B
			CP $02				; $937E
			JP Z,$93AC				; $9380
			CP $03				; $9383
			JP Z,$93BE				; $9385
			CALL $9421				; $9388
			LD B,A				; $938B
			CALL $93FC				; $938C
			OR B				; $938F
			JP Z,$92F7				; $9390
			LD (IX+$04),$00				; $9393
			JP $927F				; $9397

			CALL $93FC				; $939A
			LD B,A				; $939D
			CALL $940F				; $939E
			OR B				; $93A1
			JP Z,$92F7				; $93A2
			LD (IX+$04),$00				; $93A5
			JP $927F				; $93A9

			CALL $940F				; $93AC
			LD B,A				; $93AF
			CALL $93EA				; $93B0
			OR B				; $93B3
			JP Z,$92F7				; $93B4
			LD (IX+$04),$00				; $93B7
			JP $927F				; $93BB

			CALL $93EA				; $93BE
			LD B,A				; $93C1
			CALL $9421				; $93C2
			OR B				; $93C5
			JP Z,$92F7				; $93C6
			LD (IX+$04),$00				; $93C9
			JP $927F				; $93CD

			PUSH BC				; $93D0
			PUSH HL				; $93D1
			CALL $93FC				; $93D2
			LD H,A				; $93D5
			CALL $93EA				; $93D6
			LD L,A				; $93D9
			CALL $9421				; $93DA
			LD B,A				; $93DD
			CALL $940F				; $93DE
			LD C,A				; $93E1
			XOR A				; $93E2
			OR H				; $93E3
			OR L				; $93E4
			OR B				; $93E5
			OR C				; $93E6
			POP HL				; $93E7
			POP BC				; $93E8
			RET				; $93E9

			PUSH DE				; $93EA
			LD A,E				; $93EB
			CP $78				; $93EC
			JR NC,$93F9				; $93EE
			INC E				; $93F0
			INC E				; $93F1
			INC D				; $93F2
			INC D				; $93F3
			INC D				; $93F4
			INC D				; $93F5
			CALL $8115				; $93F6
			OR A				; $93F9
			POP DE				; $93FA
			RET				; $93FB

			PUSH DE				; $93FC
			LD A,E				; $93FD
			OR A				; $93FE
			LD A,$01				; $93FF
			JR Z,$940C				; $9401
			INC E				; $9403
			INC E				; $9404
			INC D				; $9405
			INC D				; $9406
			INC D				; $9407
			INC D				; $9408
			CALL $8139				; $9409
			OR A				; $940C
			POP DE				; $940D
			RET				; $940E

			PUSH DE				; $940F
			LD A,D				; $9410
			CP $B0				; $9411
			JR NC,$941E				; $9413
			INC E				; $9415
			INC E				; $9416
			INC D				; $9417
			INC D				; $9418
			INC D				; $9419
			INC D				; $941A
			CALL $815D				; $941B
			OR A				; $941E
			POP DE				; $941F
			RET				; $9420

			PUSH DE				; $9421
			LD A,D				; $9422
			CP $20				; $9423
			JR C,$9430				; $9425
			INC E				; $9427
			INC E				; $9428
			INC D				; $9429
			INC D				; $942A
			INC D				; $942B
			INC D				; $942C
			CALL $8181				; $942D
			OR A				; $9430
			POP DE				; $9431
			RET				; $9432

			CALL $66AA				; $9433
			CALL $844F				; $9436
			LD HL,$947C				; $9439
			CALL $6B29				; $943C
			LD HL,$C2F1				; $943F
			LD ($6C55),HL				; $9442
			LD HL,$949C				; $9445
			LD B,$0A				; $9448
			LD C,$47				; $944A
			LD DE,$0B08				; $944C
			PUSH BC				; $944F
			LD B,$10				; $9450
			LD A,(HL)				; $9452
			CALL $6C4A				; $9453
			INC HL				; $9456
			INC E				; $9457
			DJNZ $9452				; $9458
			POP BC				; $945A
			INC D				; $945B
			LD E,$08				; $945C
			DJNZ $944F				; $945E
			CALL $6672				; $9460
			CALL NZ,$84F5				; $9463
			JP NZ,$9460				; $9466
			LD BC,$00AF				; $9469
			PUSH BC				; $946C
			CALL $84F5				; $946D
			CALL $662B				; $9470
			OR A				; $9473
			POP BC				; $9474
			RET NZ				; $9475
			DEC BC				; $9476
			LD A,B				; $9477
			OR C				; $9478
			RET Z				; $9479
			JR $946C				; $947A

			defb $E6,$F1,$C2,$DF,$09,$05,$E0                    ; $947C .......
			defb $44,$43,$59,$42,$45,$52,$4E,$4F				; $9483 DCYBERNO
			defb $49,$44,$20,$48,$41,$4C,$4C,$20				; $948B ID HALL 
			defb $4F,$46,$20,$46,$41,$4D,$45,$FF				; $9493 OF FAME.
			defb $00,$52,$41,$46,$46,$41,$45,$4C				; $949B .RAFFAEL
			defb $45,$20,$20,$30,$31,$35,$30,$30				; $94A3 E  01500
			defb $30,$53,$55,$52,$59,$41,$4E,$49				; $94AB 0SURYANI
			defb $20,$20,$20,$30,$31,$30,$30,$30				; $94B3    01000
			defb $30,$42,$4F,$4E,$4E,$49,$45,$20				; $94BB 0BONNIE 
			defb $20,$20,$20,$30,$30,$35,$30,$30				; $94C3    00500
			defb $30,$51,$55,$45,$45,$4E,$20,$20				; $94CB 0QUEEN  
			defb $20,$20,$20,$30,$30,$32,$35,$30				; $94D3    00250
			defb $30,$4E,$49,$43,$4B,$20,$42,$4F				; $94DB 0NICK BO
			defb $59,$20,$20,$30,$30,$31,$30,$30				; $94E3 Y  00100
			defb $30,$53,$41,$4E,$44,$52,$41,$20				; $94EB 0SANDRA 
			defb $20,$20,$20,$30,$30,$31,$30,$30				; $94F3    00100
			defb $30,$44,$41,$56,$49,$44,$2E,$50				; $94FB 0DAVID.P
			defb $2E,$20,$20,$30,$30,$31,$30,$30				; $9503 .  00100
			defb $30,$53,$48,$41,$52,$4B,$59,$20				; $950B 0SHARKY 
			defb $20,$20,$20,$30,$30,$31,$30,$30				; $9513    00100
			defb $30,$45,$4D,$49,$4C,$59,$20,$20				; $951B 0EMILY  
			defb $20,$20,$20,$30,$30,$31,$30,$30				; $9523    00100
			defb $30,$4F,$4E,$49,$4F,$4E,$20,$20				; $952B 0ONION  
			defb $20,$20,$20,$30,$30,$31,$30,$30				; $9533    00100
			defb $30,$FF,$01,$A8,$00,$8B,$01,$A8				; $953B 0.......
			defb $45,$01,$01,$A8,$45,$02,$A8,$A8				; $9543 E...E...
			defb $45,$02,$00,$00                                ; $954B E...

			LD IX,$949C				; $954F
			BIT 7,(IX+$00)				; $9553
			JP NZ,$9648				; $9557
			PUSH IX				; $955A
			LD DE,$000A				; $955C
			ADD IX,DE				; $955F
			LD HL,$78F9				; $9561
			LD B,$06				; $9564
			LD A,(IX+$00)				; $9566
			CP (HL)				; $9569
			JP Z,$9637				; $956A
			JP NC,$963E				; $956D
			POP HL				; $9570
			PUSH HL				; $9571
			LD A,$FF				; $9572
			LD BC,$03E8				; $9574
			CPIR				; $9577
			LD HL,$03E8				; $9579
			AND A				; $957C
			SBC HL,BC				; $957D
			PUSH HL				; $957F
			POP BC				; $9580
			LD DE,$954B				; $9581
			LD HL,$953B				; $9584
			LDDR				; $9587
			LD A,$FF				; $9589
			LD ($953C),A				; $958B
			POP HL				; $958E
			PUSH HL				; $958F
			LD DE,$000A				; $9590
			ADD HL,DE				; $9593
			EX DE,HL				; $9594
			LD HL,$78F9				; $9595
			LD BC,$0006				; $9598
			LDIR				; $959B
			CALL $66AA				; $959D
			LD E,$28				; $95A0
			CALL $EF42				; $95A2
			LD HL,$96D0				; $95A5
			LD DE,$96D1				; $95A8
			LD BC,$0007				; $95AB
			LD (HL),$20				; $95AE
			LDIR				; $95B0
			CALL $844F				; $95B2
			LD HL,$9664				; $95B5
			CALL $6B29				; $95B8
			LD DE,$0F0C				; $95BB
			LD HL,$96D0				; $95BE
			LD C,$44				; $95C1
			LD A,$3F				; $95C3
			CALL $6C4A				; $95C5
			LD BC,$03E8				; $95C8
			CALL $676A				; $95CB
			CALL $6672				; $95CE
			CALL NZ,$84F5				; $95D1
			JR NZ,$95CE				; $95D4
			PUSH DE				; $95D6
			CALL $662B				; $95D7
			OR A				; $95DA
			CALL Z,$84F5				; $95DB
			JR Z,$95D7				; $95DE
			POP DE				; $95E0
			CP $0D				; $95E1
			JR Z,$961F				; $95E3
			CP $01				; $95E5
			JR Z,$95F0				; $95E7
			CP $02				; $95E9
			JR NZ,$9610				; $95EB
			PUSH DE				; $95ED
			JR $95D7				; $95EE

			LD A,E				; $95F0
			CP $13				; $95F1
			JR NZ,$95FE				; $95F3
			LD A,(HL)				; $95F5
			CP $20				; $95F6
			JR Z,$95FE				; $95F8
			LD (HL),$20				; $95FA
			JR $95C1				; $95FC

			LD A,E				; $95FE
			CP $0C				; $95FF
			JR Z,$95C1				; $9601
			LD A,$2D				; $9603
			LD C,$47				; $9605
			CALL $6C4A				; $9607
			DEC HL				; $960A
			LD (HL),$20				; $960B
			DEC E				; $960D
			JR $95C1				; $960E

			LD (HL),A				; $9610
			LD C,$46				; $9611
			CALL $6C4A				; $9613
			LD A,E				; $9616
			CP $13				; $9617
			JR Z,$95C8				; $9619
			INC HL				; $961B
			INC E				; $961C
			JR $95C1				; $961D

			POP DE				; $961F
			LD HL,$96D0				; $9620
			LD BC,$0008				; $9623
			LDIR				; $9626
			CALL $6672				; $9628
			CALL NZ,$84F5				; $962B
			JP NZ,$9628				; $962E
			CALL $9433				; $9631
			JP $6504				; $9634

			INC HL				; $9637
			INC IX				; $9638
			DEC B				; $963A
			JP NZ,$9566				; $963B
			POP IX				; $963E
			LD DE,$0010				; $9640
			ADD IX,DE				; $9643
			JP $9553				; $9645

			CALL $6672				; $9648
			JP NZ,$9648				; $964B
			LD BC,$0352				; $964E
			HALT				; $9651
			PUSH BC				; $9652
			CALL $662B				; $9653
			POP BC				; $9656
			OR A				; $9657
			JP NZ,$6504				; $9658
			DEC BC				; $965B
			LD A,B				; $965C
			OR C				; $965D
			JP NZ,$9651				; $965E
			JP $6504				; $9661

			defb $EB,$00,$DF,$09,$08,$E0,$46                    ; $9664 ......F
			defb $E6,$F1,$C2,$43,$4F,$4E,$47,$52				; $966B ...CONGR
			defb $41,$54,$55,$4C,$41,$54,$49,$4F				; $9673 ATULATIO
			defb $4E,$53,$21,$7A,$ED,$DB,$50,$4C				; $967B NS!z..PL
			defb $45,$41,$53,$45,$20,$45,$4E,$54				; $9683 EASE ENT
			defb $45,$52,$20,$59,$4F,$55,$52,$20				; $968B ER YOUR 
			defb $4E,$41,$4D,$45,$DF,$0F,$0C,$DE				; $9693 NAME....
			defb $2D,$2D,$2D,$2D,$2D,$2D,$2D,$2D				; $969B --------
			defb $7B,$F2,$DC,$50,$52,$45,$53,$53				; $96A3 {..PRESS
			defb $20,$43,$41,$50,$53,$20,$54,$4F				; $96AB  CAPS TO
			defb $20,$44,$45,$4C,$45,$54,$45,$7A				; $96B3  DELETEz
			defb $ED,$DD,$50,$52,$45,$53,$53,$20				; $96BB ..PRESS 
			defb $45,$4E,$54,$45,$52,$20,$54,$4F				; $96C3 ENTER TO
			defb $20,$45,$4E,$44,$FF,$20,$20,$20				; $96CB  END.   
			defb $20,$20,$20,$20,$20                            ; $96D3

			LD IX,$979F				; $96D8
			LD H,A				; $96DC
			LD A,(IX+$00)				; $96DD
			CP $FF				; $96E0
			RET Z				; $96E2
			LD A,(IX+$02)				; $96E3
			OR A				; $96E6
			JR Z,$96F3				; $96E7
			PUSH BC				; $96E9
			LD BC,$0008				; $96EA
			ADD IX,BC				; $96ED
			POP BC				; $96EF
			JP $96DD				; $96F0

			LD (IX+$00),E				; $96F3
			LD (IX+$01),D				; $96F6
			LD (IX+$02),H				; $96F9
			LD DE,$C6F9				; $96FC
			LD (IX+$03),E				; $96FF
			LD (IX+$04),D				; $9702
			LD (IX+$05),L				; $9705
			LD (IX+$06),C				; $9708
			LD (IX+$07),B				; $970B
			RET				; $970E

			LD IX,$979F				; $970F
			LD A,(IX+$00)				; $9713
			CP $FF				; $9716
			RET Z				; $9718
			LD A,(IX+$02)				; $9719
			OR A				; $971C
			JP NZ,$9728				; $971D
			LD BC,$0008				; $9720
			ADD IX,BC				; $9723
			JP $9713				; $9725

			LD E,(IX+$00)				; $9728
			LD D,(IX+$01)				; $972B
			LD B,D				; $972E
			LD C,E				; $972F
			LD L,(IX+$03)				; $9730
			LD H,(IX+$04)				; $9733
			LD A,(IX+$05)				; $9736
			AND $80				; $9739
			JR NZ,$974C				; $973B
			LD A,D				; $973D
			CP $B0				; $973E
			JP Z,$9787				; $9740
			CALL $69C4				; $9743
			JP NZ,$9787				; $9746
			JP $9758				; $9749

			LD A,D				; $974C
			CP $20				; $974D
			JP Z,$9787				; $974F
			CALL $69E6				; $9752
			JP NZ,$9787				; $9755
			LD A,(IX+$05)				; $9758
			ADD A,D				; $975B
			LD D,A				; $975C
			LD (IX+$01),D				; $975D
			LD A,(IX+$02)				; $9760
			CALL $A4DB				; $9763
			PUSH DE				; $9766
			LD A,E				; $9767
			ADD A,$02				; $9768
			LD E,A				; $976A
			LD A,D				; $976B
			ADD A,$04				; $976C
			LD D,A				; $976E
			LD A,$02				; $976F
			CALL $8E31				; $9771
			POP DE				; $9774
			LD (IX+$03),L				; $9775
			LD (IX+$04),H				; $9778
			LD C,(IX+$06)				; $977B
			LD B,(IX+$07)				; $977E
			CALL $A47B				; $9781
			JP $9720				; $9784

			XOR A				; $9787
			LD (IX+$02),A				; $9788
			CALL $A4DB				; $978B
			DEC E				; $978E
			DEC E				; $978F
			LD A,D				; $9790
			SUB $04				; $9791
			LD D,A				; $9793
			LD BC,$180C				; $9794
			LD A,$0F				; $9797
			CALL $89A1				; $9799
			JP $9720				; $979C

			defb $00,$00,$00,$00                                ; $979F ....
			defb $CF,$CF,$45,$CF,$CF,$CF,$45,$33				; $97A3 ..E...E3
			defb $CF,$8B,$45,$33,$CF,$8B,$45,$8A				; $97AB ..E3..E.
			defb $CF,$8B,$45,$00,$CF,$8B,$45,$33				; $97B3 ..E...E3
			defb $CF,$03,$45,$33,$45,$03,$45,$33				; $97BB ..E3E.E3
			defb $01,$56,$45,$33,$01,$56,$45,$33				; $97C3 .VE3.VE3
			defb $00,$FC,$45,$33,$00,$FC,$45,$33				; $97CB ..E3..E3
			defb $00,$54,$45,$8A,$00,$54,$45,$00				; $97D3 .TE..TE.
			defb $00,$00,$45,$33,$00,$00,$00,$33				; $97DB ..E3...3
			defb $CF,$8A,$CF,$CF,$33,$22,$9B,$33				; $97E3 ....3".3
			defb $33,$22,$9B,$33,$FF                            ; $97EB 3".3.

			LD HL,$9879				; $97F0
			LD A,(HL)				; $97F3
			CP $FF				; $97F4
			RET Z				; $97F6
			LD E,A				; $97F7
			INC HL				; $97F8
			LD D,(HL)				; $97F9
			INC HL				; $97FA
			PUSH HL				; $97FB
			CALL $678F				; $97FC
			LD A,(HL)				; $97FF
			OR A				; $9800
			JR NZ,$9807				; $9801
			POP HL				; $9803
			JP $97F3				; $9804

			LD ($9825),A				; $9807
			LD BC,$0005				; $980A
			LD HL,$9826				; $980D
			CP (HL)				; $9810
			JR Z,$9817				; $9811
			ADD HL,BC				; $9813
			JP $9810				; $9814

			INC HL				; $9817
			LD A,(HL)				; $9818
			ADD A,E				; $9819
			LD E,A				; $981A
			INC HL				; $981B
			LD A,(HL)				; $981C
			ADD A,D				; $981D
			LD D,A				; $981E
			INC HL				; $981F
			LD A,(HL)				; $9820
			INC HL				; $9821
			LD H,(HL)				; $9822
			LD L,A				; $9823
			JP (HL)				; $9824

			defb $00,$46,$00,$00,$31,$98                        ; $9825 .F..1.
			defb $47,$00,$00,$55,$98,$FF                        ; $982B G..U..

			LD A,($6969)				; $9831
			CP E				; $9834
			JP NZ,$9803				; $9835
			LD A,($9825)				; $9838
			LD HL,$C6F9				; $983B
			CALL $A4DB				; $983E
			CALL $6DE0				; $9841
			CALL $988E				; $9844
			LD L,$FC				; $9847
			LD BC,$4547				; $9849
			LD A,($9825)				; $984C
			CALL $96D8				; $984F
			JP $9803				; $9852

			LD A,($6969)				; $9855
			CP E				; $9858
			JP NZ,$9803				; $9859
			LD A,($9825)				; $985C
			LD HL,$C6F9				; $985F
			CALL $A4DB				; $9862
			CALL $6DE0				; $9865
			CALL $988E				; $9868
			LD L,$04				; $986B
			LD BC,$4547				; $986D
			LD A,($9825)				; $9870
			CALL $96D8				; $9873
			JP $9803				; $9876


			defb $9B,$33                                        ; $9879 .3
			defb $33,$22,$9B,$33,$67,$00,$9B,$22				; $987B 3".3g.."
			defb $22,$00,$9B,$22,$33,$22,$9B,$22				; $9883 ".."3"."
			defb $33,$22,$FF                                    ; $988B 3".

			PUSH AF				; $988E
			PUSH BC				; $988F
			PUSH IX				; $9890
			PUSH HL				; $9892
			LD BC,$0008				; $9893
			LD IX,$6CFC				; $9896
			LD L,(IX+$00)				; $989A
			LD H,(IX+$01)				; $989D
			AND A				; $98A0
			SBC HL,DE				; $98A1
			JR Z,$98AA				; $98A3
			ADD IX,BC				; $98A5
			JP $989A				; $98A7

			LD (IX+$04),$00				; $98AA
			POP HL				; $98AE
			POP IX				; $98AF
			POP BC				; $98B1
			POP AF				; $98B2
			RET				; $98B3

			LD IX,$99B3				; $98B4
			LD A,(IX+$00)				; $98B8
			CP $FF				; $98BB
			RET Z				; $98BD
			OR A				; $98BE
			JP Z,$999B				; $98BF
			LD E,(IX+$01)				; $98C2
			LD D,(IX+$02)				; $98C5
			PUSH DE				; $98C8
			LD C,(IX+$05)				; $98C9
			LD B,(IX+$06)				; $98CC
			LD A,(IX+$07)				; $98CF
			OR A				; $98D2
			JR NZ,$98F5				; $98D3
			LD A,(BC)				; $98D5
			CP $E2				; $98D6
			JP Z,$9902				; $98D8
			CP $E1				; $98DB
			JP Z,$9911				; $98DD
			CP $E0				; $98E0
			JP Z,$9920				; $98E2
			ADD A,E				; $98E5
			LD E,A				; $98E6
			INC BC				; $98E7
			LD A,(BC)				; $98E8
			ADD A,D				; $98E9
			LD D,A				; $98EA
			INC BC				; $98EB
			LD (IX+$05),C				; $98EC
			LD (IX+$06),B				; $98EF
			JP $9939				; $98F2

			DEC (IX+$07)				; $98F5
			LD A,(BC)				; $98F8
			ADD A,E				; $98F9
			LD E,A				; $98FA
			INC BC				; $98FB
			LD A,(BC)				; $98FC
			ADD A,D				; $98FD
			LD D,A				; $98FE
			JP $9939				; $98FF

			INC BC				; $9902
			LD A,(BC)				; $9903
			DEC A				; $9904
			LD (IX+$07),A				; $9905
			INC BC				; $9908
			LD (IX+$05),C				; $9909
			LD (IX+$06),B				; $990C
			JR $98F5				; $990F

			LD C,(IX+$03)				; $9911
			LD B,(IX+$04)				; $9914
			LD (IX+$05),C				; $9917
			LD (IX+$06),B				; $991A
			JP $98D5				; $991D

			INC BC				; $9920
			LD (IX+$05),C				; $9921
			LD (IX+$06),B				; $9924
			PUSH BC				; $9927
			LD BC,($6969)				; $9928
			LD A,$05				; $992C
			LD L,$02				; $992E
			LD H,$42				; $9930
			CALL $8C98				; $9932
			POP BC				; $9935
			JP $98D5				; $9936

			POP BC				; $9939
			LD A,(IX+$0B)				; $993A
			CP $14				; $993D
			JR NC,$9947				; $993F
			INC (IX+$0B)				; $9941
			JP $996E				; $9944

			CALL $678F				; $9947
			LD A,(HL)				; $994A
			INC L				; $994B
			OR (HL)				; $994C
			PUSH BC				; $994D
			LD BC,$001F				; $994E
			ADD HL,BC				; $9951
			POP BC				; $9952
			OR (HL)				; $9953
			INC L				; $9954
			OR (HL)				; $9955
			CALL NZ,$88EA				; $9956
			JP NZ,$99A3				; $9959
			LD A,E				; $995C
			ADD A,$08				; $995D
			CP $87				; $995F
			JP NC,$99A3				; $9961
			LD A,D				; $9964
			CP $11				; $9965
			JP C,$99A3				; $9967
			CP $C0				; $996A
			JR NC,$99A3				; $996C
			LD (IX+$01),E				; $996E
			LD (IX+$02),D				; $9971
			LD A,(IX+$00)				; $9974
			LD L,(IX+$09)				; $9977
			LD H,(IX+$0A)				; $997A
			CALL $A1FD				; $997D
			PUSH DE				; $9980
			LD A,D				; $9981
			ADD A,$04				; $9982
			LD D,A				; $9984
			LD A,E				; $9985
			ADD A,$02				; $9986
			LD E,A				; $9988
			LD A,$02				; $9989
			CALL $8E31				; $998B
			POP DE				; $998E
			LD (IX+$09),L				; $998F
			LD (IX+$0A),H				; $9992
			LD C,(IX+$08)				; $9995
			CALL $A446				; $9998
			LD DE,$000C				; $999B
			ADD IX,DE				; $999E
			JP $98B8				; $99A0

			XOR A				; $99A3
			LD (IX+$00),A				; $99A4
			LD L,(IX+$09)				; $99A7
			LD H,(IX+$0A)				; $99AA
			CALL $A1FD				; $99AD
			JP $999B				; $99B0

			defb $00,$8B,$FC,$FC,$00,$45,$56,$00				; $99B3 .....EV.
			defb $00,$45,$56,$00,$00,$00,$8B,$A8				; $99BB .EV.....
			defb $00,$00,$8B,$A8,$00,$00,$45,$56				; $99C3 ......EV
			defb $00,$00,$45,$56,$00,$00,$00,$8B				; $99CB ..EV....
			defb $00,$00,$00,$8B,$00,$00,$00,$45				; $99D3 .......E
			defb $00,$00,$00,$45,$00,$00,$00,$00				; $99DB ...E....
			defb $CF,$CF,$9B,$DE,$03,$03,$45,$8B				; $99E3 ......E.
			defb $FC,$FC,$00,$8B,$FC,$A8,$00,$8B				; $99EB ........
			defb $FC,$A8,$00,$8B,$00,$00,$00,$8B				; $99F3 ........
			defb $00,$00,$00,$8B,$00,$00,$00,$8B				; $99FB ........
			defb $00,$00,$00,$8B,$00,$00,$00,$8B				; $9A03 ........
			defb $00,$00,$00,$8B,$A8,$00,$00,$8B				; $9A0B ........
			defb $A8,$00,$00,$8B,$56,$00,$00,$8B				; $9A13 ....V...
			defb $56,$00,$00,$8B,$8B,$A8,$00,$8B				; $9A1B V.......
			defb $FC,$00,$45,$CF,$FC,$00,$45,$03				; $9A23 ..E...E.
			defb $FF                                            ; $9A2B .

			LD IX,$99B3				; $9A2C
			LD C,A				; $9A30
			LD A,(IX+$00)				; $9A31
			CP $FF				; $9A34
			RET Z				; $9A36
			OR A				; $9A37
			JR Z,$9A44				; $9A38
			PUSH BC				; $9A3A
			LD BC,$000C				; $9A3B
			ADD IX,BC				; $9A3E
			POP BC				; $9A40
			JP $9A31				; $9A41

			LD (IX+$00),C				; $9A44
			LD (IX+$01),E				; $9A47
			LD (IX+$02),D				; $9A4A
			LD (IX+$03),L				; $9A4D
			LD (IX+$04),H				; $9A50
			LD (IX+$05),L				; $9A53
			LD (IX+$06),H				; $9A56
			LD (IX+$07),$00				; $9A59
			LD A,$04				; $9A5D
			CALL $6679				; $9A5F
			ADD A,$42				; $9A62
			LD (IX+$08),A				; $9A64
			LD DE,$C6F9				; $9A67
			LD (IX+$09),E				; $9A6A
			LD (IX+$0A),D				; $9A6D
			LD (IX+$0B),$00				; $9A70
			RET				; $9A74

			PUSH BC				; $9A75
			PUSH DE				; $9A76
			PUSH HL				; $9A77
			LD B,D				; $9A78
			LD C,E				; $9A79
			LD HL,$99B3				; $9A7A
			LD A,(HL)				; $9A7D
			CP $FF				; $9A7E
			JP Z,$9AC2				; $9A80
			OR A				; $9A83
			JR NZ,$9A8D				; $9A84
			LD DE,$000C				; $9A86
			ADD HL,DE				; $9A89
			JP $9A7D				; $9A8A

			PUSH HL				; $9A8D
			INC HL				; $9A8E
			LD E,(HL)				; $9A8F
			INC HL				; $9A90
			LD D,(HL)				; $9A91
			CALL $6802				; $9A92
			POP HL				; $9A95
			OR A				; $9A96
			JP Z,$9A86				; $9A97
			LD A,(HL)				; $9A9A
			LD (HL),$00				; $9A9B
			LD HL,$C6F9				; $9A9D
			CALL $A1FD				; $9AA0
			CALL $88EA				; $9AA3
			LD A,$01				; $9AA6
			CALL $6679				; $9AA8
			OR A				; $9AAB
			JP Z,$9AB8				; $9AAC
			LD A,$0A				; $9AAF
			CALL $6679				; $9AB1
			INC A				; $9AB4
			CALL $9FA2				; $9AB5
			LD DE,$7904				; $9AB8
			CALL $78D4				; $9ABB
			CALL $788E				; $9ABE
			XOR A				; $9AC1
			INC A				; $9AC2
			POP HL				; $9AC3
			POP DE				; $9AC4
			POP BC				; $9AC5
			RET				; $9AC6

			LD IX,$9B27				; $9AC7
			LD DE,$2000				; $9ACB
			LD B,$14				; $9ACE
			CALL $678F				; $9AD0
			LD A,(HL)				; $9AD3
			OR A				; $9AD4
			JR NZ,$9AE0				; $9AD5
			LD (IX+$00),D				; $9AD7
			INC IX				; $9ADA
			LD HL,$9B47				; $9ADC
			INC (HL)				; $9ADF
			LD A,D				; $9AE0
			ADD A,$08				; $9AE1
			LD D,A				; $9AE3
			DJNZ $9AD0				; $9AE4
			RET				; $9AE6

			LD IX,$9B27				; $9AE7
			LD DE,$2078				; $9AEB
			LD B,$14				; $9AEE
			CALL $678F				; $9AF0
			LD A,(HL)				; $9AF3
			OR A				; $9AF4
			JR NZ,$9B00				; $9AF5
			LD (IX+$00),D				; $9AF7
			INC IX				; $9AFA
			LD HL,$9B47				; $9AFC
			INC (HL)				; $9AFF
			LD A,D				; $9B00
			ADD A,$08				; $9B01
			LD D,A				; $9B03
			DJNZ $9AF0				; $9B04
			RET				; $9B06

			LD IX,$9B27				; $9B07
			LD DE,$2000				; $9B0B
			LD B,$20				; $9B0E
			CALL $678F				; $9B10
			LD A,(HL)				; $9B13
			OR A				; $9B14
			JR NZ,$9B20				; $9B15
			LD (IX+$00),E				; $9B17
			INC IX				; $9B1A
			LD HL,$9B47				; $9B1C
			INC (HL)				; $9B1F
			LD A,E				; $9B20
			ADD A,$04				; $9B21
			LD E,A				; $9B23
			DJNZ $9B10				; $9B24
			RET				; $9B26

			defb $8B,$03,$03,$00                                ; $9B27 ....
			defb $56,$FC,$FC,$A8,$FC,$00,$DE,$A8				; $9B2B V.......
			defb $A8,$00,$DE,$ED,$A8,$45,$FC,$45				; $9B33 .....E.E
			defb $00,$45,$FC,$8B,$00,$DE,$A8,$8B				; $9B3B .E......
			defb $00,$DE,$ED,$56,$00                            ; $9B43 ...V.

			LD IX,$9B27				; $9B48
			LD DE,$B000				; $9B4C
			LD B,$20				; $9B4F
			CALL $678F				; $9B51
			LD A,(HL)				; $9B54
			OR A				; $9B55
			JR NZ,$9B61				; $9B56
			LD (IX+$00),E				; $9B58
			INC IX				; $9B5B
			LD HL,$9B47				; $9B5D
			INC (HL)				; $9B60
			LD A,E				; $9B61
			ADD A,$04				; $9B62
			LD E,A				; $9B64
			DJNZ $9B51				; $9B65
			RET				; $9B67

			CP $EC				; $9B68
			RET C				; $9B6A
			CP $F0				; $9B6B
			RET NC				; $9B6D
			PUSH AF				; $9B6E
			PUSH BC				; $9B6F
			PUSH DE				; $9B70
			PUSH HL				; $9B71
			SUB $EC				; $9B72
			LD ($9BD5),A				; $9B74
			ADD A,A				; $9B77
			ADD A,A				; $9B78
			LD L,A				; $9B79
			LD H,$00				; $9B7A
			LD BC,$9BBF				; $9B7C
			ADD HL,BC				; $9B7F
			LD BC,$0004				; $9B80
			LD DE,$9BCF				; $9B83
			LDIR				; $9B86
			LD A,($9BD5)				; $9B88
			ADD A,A				; $9B8B
			LD L,A				; $9B8C
			LD H,$00				; $9B8D
			LD BC,$9C56				; $9B8F
			ADD HL,BC				; $9B92
			LD C,(HL)				; $9B93
			INC HL				; $9B94
			LD H,(HL)				; $9B95
			LD L,C				; $9B96
			LD A,$03				; $9B97
			CALL $6679				; $9B99
			ADD A,$0D				; $9B9C
			LD ($9C55),A				; $9B9E
			LD A,$03				; $9BA1
			CALL $6679				; $9BA3
			ADD A,A				; $9BA6
			LD C,A				; $9BA7
			LD B,$00				; $9BA8
			ADD HL,BC				; $9BAA
			LD A,(HL)				; $9BAB
			INC HL				; $9BAC
			LD H,(HL)				; $9BAD
			LD L,A				; $9BAE
			LD ($9BD6),HL				; $9BAF
			LD A,($9BD4)				; $9BB2
			SUB $0A				; $9BB5
			LD ($9BD4),A				; $9BB7
			POP HL				; $9BBA
			POP DE				; $9BBB
			POP BC				; $9BBC
			POP AF				; $9BBD
			RET				; $9BBE

			defb $E7,$9A,$01,$7F                                ; $9BBF ....
			defb $48,$9B,$00,$BE,$C7,$9A,$01,$F9				; $9BC3 H.......
			defb $07,$9B,$00,$11,$00,$00,$00,$00				; $9BCB ........
			defb $00,$00,$00,$00,$00                            ; $9BD3 .....

			LD A,($9BD5)				; $9BD8
			CP $FF				; $9BDB
			RET Z				; $9BDD
			LD HL,($9BCF)				; $9BDE
			JP (HL)				; $9BE1
			LD A,($9BD5)				; $9BE2
			CP $FF				; $9BE5
			RET Z				; $9BE7
			LD A,($9BD3)				; $9BE8
			DEC A				; $9BEB
			LD ($9BD3),A				; $9BEC
			OR A				; $9BEF
			RET NZ				; $9BF0
			LD A,($9BD4)				; $9BF1
			LD ($9BD3),A				; $9BF4
			LD A,($9B47)				; $9BF7
			CALL $6679				; $9BFA
			LD L,A				; $9BFD
			LD H,$00				; $9BFE
			LD BC,$9B27				; $9C00
			ADD HL,BC				; $9C03
			LD A,($9BD1)				; $9C04
			OR A				; $9C07
			LD A,($9BD2)				; $9C08
			JR Z,$9C30				; $9C0B
			LD D,(HL)				; $9C0D
			LD E,A				; $9C0E
			LD A,($9BD5)				; $9C0F
			OR A				; $9C12
			LD A,($6969)				; $9C13
			JR Z,$9C24				; $9C16
			CP $10				; $9C18
			RET C				; $9C1A
			LD HL,($9BD6)				; $9C1B
			LD A,($9C55)				; $9C1E
			JP $9A2C				; $9C21

			CP $68				; $9C24
			RET NC				; $9C26
			LD HL,($9BD6)				; $9C27
			LD A,($9C55)				; $9C2A
			JP $9A2C				; $9C2D

			LD E,(HL)				; $9C30
			LD D,A				; $9C31
			LD A,($9BD5)				; $9C32
			CP $01				; $9C35
			LD A,($696A)				; $9C37
			JP Z,$9C49				; $9C3A
			CP $40				; $9C3D
			RET C				; $9C3F
			LD HL,($9BD6)				; $9C40
			LD A,($9C55)				; $9C43
			JP $9A2C				; $9C46

			CP $90				; $9C49
			RET NC				; $9C4B
			LD HL,($9BD6)				; $9C4C
			LD A,($9C55)				; $9C4F
			JP $9A2C				; $9C52

			defb $00,$5E,$9C,$66,$9C,$6E                        ; $9C55 .^.f.n
			defb $9C,$76,$9C,$EC,$9D,$26,$9E,$51				; $9C5B .v...&.Q
			defb $9E,$88,$9E,$7E,$9C,$C3,$9C,$E5				; $9C63 ...~....
			defb $9C,$03,$9D,$C7,$9E,$01,$9F,$2C				; $9C6B .......,
			defb $9F,$63,$9F,$35,$9D,$7A,$9D,$9C				; $9C73 .c.5.z..
			defb $9D,$BA,$9D,$E2,$11,$00,$FE,$00				; $9C7B ........
			defb $FF,$00,$FF,$00,$FF,$FF,$FF,$FF				; $9C83 ........
			defb $FF,$FE,$FF,$FE,$FF,$FD,$FF,$FD				; $9C8B ........
			defb $FF,$FD,$FF,$FD,$FF,$FD,$FF,$FD				; $9C93 ........
			defb $FF,$FE,$FF,$FE,$FF,$FF,$FF,$FF				; $9C9B ........
			defb $FF,$00,$FF,$01,$FF,$01,$FF,$02				; $9CA3 ........
			defb $FF,$02,$FF,$03,$FF,$03,$FF,$03				; $9CAB ........
			defb $FF,$03,$FF,$03,$FF,$03,$FF,$02				; $9CB3 ........
			defb $FF,$02,$FF,$01,$FF,$01,$FF,$E1				; $9CBB ........
			defb $E2,$08,$00,$FC,$E2,$0C,$00,$FE				; $9CC3 ........
			defb $E2,$08,$01,$FE,$E2,$08,$00,$FE				; $9CCB ........
			defb $E2,$08,$FF,$FE,$E0,$E2,$0A,$FE				; $9CD3 ........
			defb $00,$E2,$04,$02,$04,$E2,$32,$00				; $9CDB ......2.
			defb $FE,$E1,$E2,$0C,$01,$FE,$E2,$0C				; $9CE3 ........
			defb $FF,$FE,$E2,$0C,$01,$FE,$E2,$0C				; $9CEB ........
			defb $FF,$FE,$E0,$E2,$0C,$01,$FE,$E2				; $9CF3 ........
			defb $0C,$FF,$FE,$E2,$0C,$01,$FE,$E1				; $9CFB ........
			defb $E2,$12,$00,$FE,$E2,$08,$01,$FE				; $9D03 ........
			defb $E2,$10,$FF,$FE,$E2,$08,$00,$FE				; $9D0B ........
			defb $E2,$08,$01,$FE,$E2,$08,$01,$00				; $9D13 ........
			defb $E0,$E2,$08,$01,$02,$E2,$08,$00				; $9D1B ........
			defb $03,$E2,$0A,$00,$04,$E2,$08,$FF				; $9D23 ........
			defb $02,$E2,$08,$FF,$FE,$E2,$28,$00				; $9D2B ......(.
			defb $FC,$E1,$E2,$11,$00,$02,$00,$01				; $9D33 ........
			defb $00,$01,$00,$01,$FF,$01,$FF,$01				; $9D3B ........
			defb $FE,$01,$FE,$01,$FD,$01,$FD,$01				; $9D43 ........
			defb $FD,$01,$FD,$01,$FD,$01,$FD,$01				; $9D4B ........
			defb $FE,$01,$FE,$01,$FF,$01,$FF,$01				; $9D53 ........
			defb $00,$01,$01,$01,$01,$01,$02,$01				; $9D5B ........
			defb $02,$01,$03,$01,$03,$01,$03,$01				; $9D63 ........
			defb $03,$01,$03,$01,$03,$01,$02,$01				; $9D6B ........
			defb $02,$01,$01,$01,$01,$01,$E1,$E2				; $9D73 ........
			defb $08,$00,$04,$E2,$0C,$00,$02,$E2				; $9D7B ........
			defb $08,$01,$02,$E2,$08,$00,$02,$E2				; $9D83 ........
			defb $08,$FF,$02,$E0,$E2,$0A,$FE,$00				; $9D8B ........
			defb $E2,$04,$02,$FC,$E2,$32,$00,$02				; $9D93 .....2..
			defb $E1,$E2,$0C,$01,$02,$E2,$0C,$FF				; $9D9B ........
			defb $02,$E2,$0C,$01,$02,$E2,$0C,$FF				; $9DA3 ........
			defb $02,$E0,$E2,$0C,$01,$02,$E2,$0C				; $9DAB ........
			defb $FF,$02,$E2,$0C,$01,$02,$E1,$E2				; $9DB3 ........
			defb $12,$00,$02,$E2,$08,$01,$02,$E2				; $9DBB ........
			defb $10,$FF,$02,$E2,$08,$00,$02,$E2				; $9DC3 ........
			defb $08,$01,$02,$E2,$08,$01,$00,$E0				; $9DCB ........
			defb $E2,$08,$01,$FE,$E2,$08,$00,$FD				; $9DD3 ........
			defb $E2,$0A,$00,$FC,$E2,$08,$FF,$FE				; $9DDB ........
			defb $E2,$08,$FF,$02,$E2,$28,$00,$04				; $9DE3 .....(..
			defb $E1,$E2,$07,$FC,$00,$E2,$06,$FE				; $9DEB ........
			defb $00,$E2,$0C,$FF,$00,$E2,$04,$FF				; $9DF3 ........
			defb $02,$E0,$E2,$04,$00,$02,$E2,$04				; $9DFB ........
			defb $01,$02,$E2,$04,$01,$00,$E2,$04				; $9E03 ........
			defb $01,$FE,$E2,$04,$00,$FE,$E2,$0A				; $9E0B ........
			defb $FE,$00,$E2,$0C,$FF,$FE,$E2,$0C				; $9E13 ........
			defb $FF,$02,$E2,$08,$FE,$00,$E2,$0A				; $9E1B ........
			defb $FC,$00,$E1,$E2,$0C,$FE,$00,$E2				; $9E23 ........
			defb $06,$FE,$04,$E2,$06,$FE,$FC,$E2				; $9E2B ........
			defb $06,$FE,$04,$E2,$0C,$00,$FE,$E2				; $9E33 ........
			defb $0F,$00,$00,$E0,$E2,$10,$FF,$00				; $9E3B ........
			defb $E0,$E2,$04,$FE,$00,$E2,$04,$FD				; $9E43 ........
			defb $00,$E2,$0B,$FC,$00,$E1,$E2,$07				; $9E4B ........
			defb $FC,$00,$E2,$06,$FE,$00,$E2,$0C				; $9E53 ........
			defb $FF,$00,$E2,$04,$FF,$02,$E0,$E2				; $9E5B ........
			defb $08,$FF,$FE,$E2,$08,$00,$FE,$E2				; $9E63 ........
			defb $08,$01,$FE,$E2,$08,$01,$00,$E2				; $9E6B ........
			defb $08,$00,$02,$E2,$08,$FF,$02,$E0				; $9E73 ........
			defb $E2,$0C,$FF,$00,$E2,$08,$FE,$00				; $9E7B ........
			defb $E2,$0D,$FC,$00,$E1,$FF,$00,$FF				; $9E83 ........
			defb $00,$FF,$FF,$FF,$FF,$FF,$FE,$FF				; $9E8B ........
			defb $FE,$FF,$FD,$FF,$FD,$FF,$FD,$FF				; $9E93 ........
			defb $FD,$FF,$FD,$FF,$FD,$FF,$FE,$FF				; $9E9B ........
			defb $FE,$FF,$FF,$FF,$FF,$FF,$00,$FF				; $9EA3 ........
			defb $01,$FF,$01,$FF,$02,$FF,$02,$FF				; $9EAB ........
			defb $03,$FF,$03,$FF,$03,$FF,$03,$FF				; $9EB3 ........
			defb $03,$FF,$03,$FF,$02,$FF,$02,$FF				; $9EBB ........
			defb $01,$FF,$01,$E1,$E2,$07,$04,$00				; $9EC3 ........
			defb $E2,$06,$02,$00,$E2,$0C,$01,$00				; $9ECB ........
			defb $E2,$04,$01,$02,$E0,$E2,$04,$00				; $9ED3 ........
			defb $02,$E2,$04,$FF,$02,$E2,$04,$FF				; $9EDB ........
			defb $00,$E2,$04,$FF,$FE,$E2,$04,$00				; $9EE3 ........
			defb $FE,$E2,$0A,$02,$00,$E2,$0C,$01				; $9EEB ........
			defb $FE,$E2,$0C,$01,$02,$E2,$08,$02				; $9EF3 ........
			defb $00,$E2,$0A,$04,$00,$E1,$E2,$0C				; $9EFB ........
			defb $02,$00,$E2,$06,$02,$04,$E2,$06				; $9F03 ........
			defb $02,$FC,$E2,$06,$02,$04,$E2,$0C				; $9F0B ........
			defb $00,$FE,$E2,$0F,$00,$00,$E0,$E2				; $9F13 ........
			defb $10,$01,$00,$E0,$E2,$04,$02,$00				; $9F1B ........
			defb $E2,$04,$03,$00,$E2,$0B,$04,$00				; $9F23 ........
			defb $E1,$E2,$07,$04,$00,$E2,$06,$02				; $9F2B ........
			defb $00,$E2,$0C,$01,$00,$E2,$04,$01				; $9F33 ........
			defb $02,$E0,$E2,$08,$01,$FE,$E2,$08				; $9F3B ........
			defb $00,$FE,$E2,$08,$FF,$FE,$E2,$08				; $9F43 ........
			defb $FF,$00,$E2,$08,$00,$02,$E2,$08				; $9F4B ........
			defb $01,$02,$E0,$E2,$0C,$01,$00,$E2				; $9F53 ........
			defb $08,$02,$00,$E2,$0D,$04,$00,$E1				; $9F5B ........
			defb $01,$00,$01,$00,$01,$FF,$01,$FF				; $9F63 ........
			defb $01,$FE,$01,$FE,$01,$FD,$01,$FD				; $9F6B ........
			defb $01,$FD,$01,$FD,$01,$FD,$01,$FD				; $9F73 ........
			defb $01,$FE,$01,$FE,$01,$FF,$01,$FF				; $9F7B ........
			defb $01,$00,$01,$01,$01,$01,$01,$02				; $9F83 ........
			defb $01,$02,$01,$03,$01,$03,$01,$03				; $9F8B ........
			defb $01,$03,$01,$03,$01,$03,$01,$02				; $9F93 ........
			defb $01,$02,$01,$01,$01,$01,$E1                    ; $9F9B .......

			PUSH AF				; $9FA2
			PUSH BC				; $9FA3
			PUSH DE				; $9FA4
			PUSH HL				; $9FA5
			PUSH IX				; $9FA6
			LD C,A				; $9FA8
			LD A,E				; $9FA9
			CP $79				; $9FAA
			JP NC,$9FFB				; $9FAC
			LD A,D				; $9FAF
			AND $F8				; $9FB0
			LD D,A				; $9FB2
			CALL $678F				; $9FB3
			LD A,(HL)				; $9FB6
			INC L				; $9FB7
			OR (HL)				; $9FB8
			PUSH DE				; $9FB9
			LD DE,$001F				; $9FBA
			ADD HL,DE				; $9FBD
			POP DE				; $9FBE
			OR (HL)				; $9FBF
			INC L				; $9FC0
			OR (HL)				; $9FC1
			JP NZ,$9FFB				; $9FC2
			LD IX,$A002				; $9FC5
			LD A,(IX+$00)				; $9FC9
			CP $FF				; $9FCC
			JP Z,$9FFB				; $9FCE
			OR A				; $9FD1
			JR Z,$9FDE				; $9FD2
			PUSH BC				; $9FD4
			LD BC,$0006				; $9FD5
			ADD IX,BC				; $9FD8
			POP BC				; $9FDA
			JP $9FC9				; $9FDB

			LD A,C				; $9FDE
			CALL $A03F				; $9FDF
			LD (IX+$00),C				; $9FE2
			LD A,(HL)				; $9FE5
			LD (IX+$01),A				; $9FE6
			LD A,E				; $9FE9
			AND $FC				; $9FEA
			LD (IX+$02),A				; $9FEC
			LD (IX+$03),D				; $9FEF
			LD DE,$C6F9				; $9FF2
			LD (IX+$04),E				; $9FF5
			LD (IX+$05),D				; $9FF8
			POP IX				; $9FFB
			POP HL				; $9FFD
			POP DE				; $9FFE
			POP BC				; $9FFF
			POP AF				; $A000
			RET				; $A001

			defb $04                                            ; $A002 .
			defb $45,$45,$4B,$40,$45,$45,$4B,$40				; $A003 EEK@EEK@
			defb $45,$45,$C3,$40,$45,$45,$C3,$40				; $A00B EE.@EE.@
			defb $00,$0A,$82,$80,$00,$0A,$00,$80				; $A013 ........
			defb $00,$4B,$40,$80,$00,$41,$C0,$00				; $A01B .K@..A..
			defb $00,$44,$08,$00,$00,$44,$08,$00				; $A023 .D...D..
			defb $00,$CC,$0C,$00,$00,$CC,$0C,$00				; $A02B ........
			defb $44,$88,$04,$08,$44,$88,$04,$08				; $A033 D...D...
			defb $CC,$45,$0A,$FF                                ; $A03B .E..

			PUSH AF				; $A03F
			PUSH BC				; $A040
			ADD A,A				; $A041
			ADD A,A				; $A042
			ADD A,A				; $A043
			LD L,A				; $A044
			LD H,$00				; $A045
			LD BC,$A15D				; $A047
			ADD HL,BC				; $A04A
			POP BC				; $A04B
			POP AF				; $A04C
			RET				; $A04D

			LD HL,$A002				; $A04E
			LD A,(HL)				; $A051
			CP $FF				; $A052
			RET Z				; $A054
			OR A				; $A055
			JR NZ,$A05F				; $A056
			LD BC,$0006				; $A058
			ADD HL,BC				; $A05B
			JP $A051				; $A05C

			PUSH HL				; $A05F
			INC HL				; $A060
			LD C,(HL)				; $A061
			INC HL				; $A062
			LD E,(HL)				; $A063
			INC HL				; $A064
			LD D,(HL)				; $A065
			CALL $69C4				; $A066
			JR NZ,$A082				; $A069
			LD B,D				; $A06B
			INC D				; $A06C
			LD (HL),D				; $A06D
			INC HL				; $A06E
			LD A,(HL)				; $A06F
			INC HL				; $A070
			LD H,(HL)				; $A071
			LD L,A				; $A072
			LD A,C				; $A073
			LD C,E				; $A074
			CALL $A4DB				; $A075
			POP IX				; $A078
			PUSH IX				; $A07A
			LD (IX+$04),L				; $A07C
			LD (IX+$05),H				; $A07F
			POP HL				; $A082
			JP $A058				; $A083

			LD A,$01				; $A086
			CALL $67B9				; $A088
			LD IX,$A002				; $A08B
			LD A,(IX+$00)				; $A08F
			CP $FF				; $A092
			RET Z				; $A094
			OR A				; $A095
			JR NZ,$A0A0				; $A096
			LD BC,$0006				; $A098
			ADD IX,BC				; $A09B
			JP $A08F				; $A09D

			LD E,(IX+$02)				; $A0A0
			LD D,(IX+$03)				; $A0A3
			LD B,A				; $A0A6
			LD A,D				; $A0A7
			CP $C0				; $A0A8
			LD A,B				; $A0AA
			JP NC,$A0F6				; $A0AB
			CALL $A03F				; $A0AE
			JP NC,$A0D4				; $A0B1
			LD A,(HL)				; $A0B4
			INC HL				; $A0B5
			LD C,(HL)				; $A0B6
			INC HL				; $A0B7
			LD B,(HL)				; $A0B8
			CALL $A47B				; $A0B9
			LD A,($8F94)				; $A0BC
			OR A				; $A0BF
			JR NZ,$A0CC				; $A0C0
			LD BC,($6969)				; $A0C2
			CALL $6802				; $A0C6
			OR A				; $A0C9
			JR NZ,$A0D4				; $A0CA
			LD BC,$0006				; $A0CC
			ADD IX,BC				; $A0CF
			JP $A08F				; $A0D1

			INC HL				; $A0D4
			LD E,(HL)				; $A0D5
			INC HL				; $A0D6
			LD D,(HL)				; $A0D7
			PUSH HL				; $A0D8
			PUSH IX				; $A0D9
			LD HL,($7973)				; $A0DB
			ADD HL,DE				; $A0DE
			LD ($7973),HL				; $A0DF
			CALL $7935				; $A0E2
			CALL $78AF				; $A0E5
			LD E,$19				; $A0E8
			CALL $EF42				; $A0EA
			POP IX				; $A0ED
			POP HL				; $A0EF
			INC HL				; $A0F0
			LD A,(HL)				; $A0F1
			INC HL				; $A0F2
			LD H,(HL)				; $A0F3
			LD L,A				; $A0F4
			JP (HL)				; $A0F5
			LD (IX+$00),$00				; $A0F6
			LD C,(IX+$02)				; $A0FA
			LD B,(IX+$03)				; $A0FD
			LD L,(IX+$04)				; $A100
			LD H,(IX+$05)				; $A103
			XOR A				; $A106
			CALL $A4DB				; $A107
			LD BC,$0006				; $A10A
			ADD IX,BC				; $A10D
			JP $A08F				; $A10F

			LD A,($6AEE)				; $A112
			OR A				; $A115
			JP NZ,$A0F6				; $A116
			LD A,$01				; $A119
			LD ($6AEE),A				; $A11B
			LD DE,($6969)				; $A11E
			LD B,D				; $A122
			LD C,E				; $A123
			CALL $6953				; $A124
			JP $A0F6				; $A127

			LD A,($7BFA)				; $A12A
			ADD A,A				; $A12D
			ADD A,A				; $A12E
			ADD A,A				; $A12F
			ADD A,A				; $A130
			LD H,$00				; $A131
			LD L,A				; $A133
			LD BC,$7BB5				; $A134
			ADD HL,BC				; $A137
			LD C,(HL)				; $A138
			INC C				; $A139
			INC HL				; $A13A
			INC HL				; $A13B
			INC HL				; $A13C
			INC HL				; $A13D
			LD A,(HL)				; $A13E
			CP C				; $A13F
			JP C,$A0F6				; $A140
			DEC HL				; $A143
			DEC HL				; $A144
			DEC HL				; $A145
			DEC HL				; $A146
			LD (HL),C				; $A147
			CALL $7B72				; $A148
			JP $A0F6				; $A14B

			LD A,($6AF3)				; $A14E
			OR A				; $A151
			JP NZ,$A0F6				; $A152
			LD A,$01				; $A155
			LD ($6AF3),A				; $A157
			JP $A0F6				; $A15A

			defb $00,$00,$00,$00,$00,$00                        ; $A15D ......
			defb $00,$00,$71,$47,$47,$00,$00,$12				; $A163 ..qGG...
			defb $A1,$00,$72,$45,$04,$32,$00,$F6				; $A16B ..rE.2..
			defb $A0,$00,$73,$43,$42,$50,$00,$F6				; $A173 ..sCBP..
			defb $A0,$00,$74,$47,$46,$6E,$00,$F6				; $A17B ..tGFn..
			defb $A0,$00,$75,$45,$05,$8C,$00,$F6				; $A183 ..uE....
			defb $A0,$00,$72,$45,$05,$32,$00,$F6				; $A18B ..rE.2..
			defb $A0,$00,$73,$47,$46,$50,$00,$F6				; $A193 ..sGFP..
			defb $A0,$00,$74,$43,$42,$6E,$00,$F6				; $A19B ..tCBn..
			defb $A0,$00,$75,$45,$04,$8C,$00,$F6				; $A1A3 ..uE....
			defb $A0,$00,$76,$47,$46,$00,$00,$2A				; $A1AB ..vGF..*
			defb $A1,$00,$77,$47,$07,$00,$00,$4E				; $A1B3 ..wG...N
			defb $A1,$00,$83,$47,$47,$00,$00,$F6				; $A1BB ...GG...
			defb $A0,$00,$84,$47,$07,$00,$00,$F6				; $A1C3 ...G....
			defb $A0,$00,$8C,$47,$47,$00,$00,$F6				; $A1CB ...GG...
			defb $A0,$00,$FF                                    ; $A1D3 ...

			CP $E9				; $A1D6
			RET C				; $A1D8
			PUSH AF				; $A1D9
			PUSH BC				; $A1DA
			PUSH DE				; $A1DB
			PUSH HL				; $A1DC
			LD HL,$A1F6				; $A1DD
			LD C,A				; $A1E0
			LD A,(HL)				; $A1E1
			CP $FF				; $A1E2
			JR Z,$A1F1				; $A1E4
			INC HL				; $A1E6
			INC HL				; $A1E7
			CP C				; $A1E8
			JR NZ,$A1E1				; $A1E9
			DEC HL				; $A1EB
			LD A,(HL)				; $A1EC
			DEC D				; $A1ED
			CALL $9FA2				; $A1EE
			POP HL				; $A1F1
			POP DE				; $A1F2
			POP BC				; $A1F3
			POP AF				; $A1F4
			RET				; $A1F5

			JP (HL)				; $A1F6

			defb $0A,$EA,$01,$EB                                ; $A1F7 ....
			defb $0B,$FF                                        ; $A1FB ..

			PUSH AF				; $A1FD
			PUSH BC				; $A1FE
			PUSH DE				; $A1FF
			PUSH HL				; $A200
			PUSH BC				; $A201
			LD L,A				; $A202
			LD A,E				; $A203
			CP $AA				; $A204
			JP NC,$A367				; $A206
			LD A,C				; $A209
			CP $AA				; $A20A
			JP NC,$A367				; $A20C
			LD A,E				; $A20F
			CP $79				; $A210
			JP NC,$A29C				; $A212
			LD A,C				; $A215
			CP $79				; $A216
			JP NC,$A29C				; $A218
			LD H,L				; $A21B
			LD L,$00				; $A21C
			SRL H				; $A21E
			RR L				; $A220
			LD B,H				; $A222
			LD C,L				; $A223
			SRL H				; $A224
			RR L				; $A226
			ADD HL,BC				; $A228
			LD BC,$C6F9				; $A229
			ADD HL,BC				; $A22C
			LD B,H				; $A22D
			LD C,L				; $A22E
			LD A,E				; $A22F
			AND $03				; $A230
			LD L,A				; $A232
			LD H,$00				; $A233
			ADD HL,HL				; $A235
			ADD HL,HL				; $A236
			ADD HL,HL				; $A237
			ADD HL,HL				; $A238
			PUSH DE				; $A239
			LD D,H				; $A23A
			LD E,L				; $A23B
			ADD HL,HL				; $A23C
			ADD HL,DE				; $A23D
			ADD HL,BC				; $A23E
			LD ($A29A),HL				; $A23F
			POP DE				; $A242
			LD A,E				; $A243
			AND $7C				; $A244
			RRCA				; $A246
			RRCA				; $A247
			LD ($A266),A				; $A248
			LD C,D				; $A24B
			EXX				; $A24C
			POP DE				; $A24D
			LD A,E				; $A24E
			AND $7C				; $A24F
			RRCA				; $A251
			RRCA				; $A252
			LD ($A27F),A				; $A253
			LD C,D				; $A256
			POP DE				; $A257
			EXX				; $A258
			LD DE,($A29A)				; $A259
			LD B,$10				; $A25D
			LD H,$64				; $A25F
			LD L,C				; $A261
			LD A,(HL)				; $A262
			DEC H				; $A263
			LD H,(HL)				; $A264
			OR $00				; $A265
			LD L,A				; $A267
			INC C				; $A268
			LD A,(DE)				; $A269
			XOR (HL)				; $A26A
			LD (HL),A				; $A26B
			INC L				; $A26C
			INC DE				; $A26D
			LD A,(DE)				; $A26E
			XOR (HL)				; $A26F
			LD (HL),A				; $A270
			INC L				; $A271
			INC DE				; $A272
			LD A,(DE)				; $A273
			XOR (HL)				; $A274
			LD (HL),A				; $A275
			INC DE				; $A276
			EXX				; $A277
			LD H,$64				; $A278
			LD L,C				; $A27A
			LD A,(HL)				; $A27B
			DEC H				; $A27C
			LD H,(HL)				; $A27D
			OR $00				; $A27E
			LD L,A				; $A280
			INC C				; $A281
			LD A,(DE)				; $A282
			XOR (HL)				; $A283
			LD (HL),A				; $A284
			INC L				; $A285
			INC DE				; $A286
			LD A,(DE)				; $A287
			XOR (HL)				; $A288
			LD (HL),A				; $A289
			INC L				; $A28A
			INC DE				; $A28B
			LD A,(DE)				; $A28C
			XOR (HL)				; $A28D
			LD (HL),A				; $A28E
			INC DE				; $A28F
			EXX				; $A290
			DJNZ $A25F				; $A291
			LD HL,($A29A)				; $A293
			POP DE				; $A296
			POP BC				; $A297
			POP AF				; $A298
			RET				; $A299

			LD SP,HL				; $A29A
			ADD A,$65				; $A29B
			LD L,$00				; $A29D
			SRL H				; $A29F
			RR L				; $A2A1
			LD B,H				; $A2A3
			LD C,L				; $A2A4
			SRL H				; $A2A5
			RR L				; $A2A7
			ADD HL,BC				; $A2A9
			LD BC,$C6F9				; $A2AA
			ADD HL,BC				; $A2AD
			LD B,H				; $A2AE
			LD C,L				; $A2AF
			LD A,E				; $A2B0
			AND $03				; $A2B1
			LD L,A				; $A2B3
			LD H,$00				; $A2B4
			ADD HL,HL				; $A2B6
			ADD HL,HL				; $A2B7
			ADD HL,HL				; $A2B8
			ADD HL,HL				; $A2B9
			PUSH DE				; $A2BA
			LD D,H				; $A2BB
			LD E,L				; $A2BC
			ADD HL,HL				; $A2BD
			ADD HL,DE				; $A2BE
			ADD HL,BC				; $A2BF
			LD ($A29A),HL				; $A2C0
			POP DE				; $A2C3
			LD A,E				; $A2C4
			AND $7C				; $A2C5
			RRCA				; $A2C7
			RRCA				; $A2C8
			LD ($A333),A				; $A2C9
			LD C,D				; $A2CC
			LD A,$77				; $A2CD
			LD ($A33D),A				; $A2CF
			LD ($A342),A				; $A2D2
			LD ($A338),A				; $A2D5
			LD A,E				; $A2D8
			CP $79				; $A2D9
			JR C,$A2F3				; $A2DB
			XOR A				; $A2DD
			LD ($A342),A				; $A2DE
			LD A,E				; $A2E1
			CP $7C				; $A2E2
			JR C,$A2F3				; $A2E4
			XOR A				; $A2E6
			LD ($A33D),A				; $A2E7
			LD A,E				; $A2EA
			CP $80				; $A2EB
			JR C,$A2F3				; $A2ED
			XOR A				; $A2EF
			LD ($A338),A				; $A2F0
			EXX				; $A2F3
			POP DE				; $A2F4
			LD A,$77				; $A2F5
			LD ($A356),A				; $A2F7
			LD ($A35B),A				; $A2FA
			LD ($A351),A				; $A2FD
			LD A,E				; $A300
			CP $79				; $A301
			JR C,$A31B				; $A303
			XOR A				; $A305
			LD ($A35B),A				; $A306
			LD A,E				; $A309
			CP $7C				; $A30A
			JR C,$A31B				; $A30C
			XOR A				; $A30E
			LD ($A356),A				; $A30F
			LD A,E				; $A312
			CP $80				; $A313
			JR C,$A31B				; $A315
			XOR A				; $A317
			LD ($A351),A				; $A318
			LD A,E				; $A31B
			AND $7C				; $A31C
			RRCA				; $A31E
			RRCA				; $A31F
			LD ($A34C),A				; $A320
			LD C,D				; $A323
			POP DE				; $A324
			EXX				; $A325
			LD DE,($A29A)				; $A326
			LD B,$10				; $A32A
			LD H,$64				; $A32C
			LD L,C				; $A32E
			LD A,(HL)				; $A32F
			DEC H				; $A330
			LD H,(HL)				; $A331
			OR $00				; $A332
			LD L,A				; $A334
			INC C				; $A335
			LD A,(DE)				; $A336
			XOR (HL)				; $A337
			LD (HL),A				; $A338
			INC L				; $A339
			INC DE				; $A33A
			LD A,(DE)				; $A33B
			XOR (HL)				; $A33C
			LD (HL),A				; $A33D
			INC L				; $A33E
			INC DE				; $A33F
			LD A,(DE)				; $A340
			XOR (HL)				; $A341
			LD (HL),A				; $A342
			INC DE				; $A343
			EXX				; $A344
			LD H,$64				; $A345
			LD L,C				; $A347
			LD A,(HL)				; $A348
			DEC H				; $A349
			LD H,(HL)				; $A34A
			OR $00				; $A34B
			LD L,A				; $A34D
			INC C				; $A34E
			LD A,(DE)				; $A34F
			XOR (HL)				; $A350
			LD (HL),A				; $A351
			INC L				; $A352
			INC DE				; $A353
			LD A,(DE)				; $A354
			XOR (HL)				; $A355
			LD (HL),A				; $A356
			INC L				; $A357
			INC DE				; $A358
			LD A,(DE)				; $A359
			XOR (HL)				; $A35A
			LD (HL),A				; $A35B
			INC DE				; $A35C
			EXX				; $A35D
			DJNZ $A32C				; $A35E
			LD HL,($A29A)				; $A360
			POP DE				; $A363
			POP BC				; $A364
			POP AF				; $A365
			RET				; $A366

			LD H,L				; $A367
			LD L,$00				; $A368
			SRL H				; $A36A
			RR L				; $A36C
			LD B,H				; $A36E
			LD C,L				; $A36F
			SRL H				; $A370
			RR L				; $A372
			ADD HL,BC				; $A374
			LD BC,$C6F9				; $A375
			ADD HL,BC				; $A378
			LD B,H				; $A379
			LD C,L				; $A37A
			LD A,E				; $A37B
			AND $03				; $A37C
			LD L,A				; $A37E
			LD H,$00				; $A37F
			ADD HL,HL				; $A381
			ADD HL,HL				; $A382
			ADD HL,HL				; $A383
			ADD HL,HL				; $A384
			PUSH DE				; $A385
			LD D,H				; $A386
			LD E,L				; $A387
			ADD HL,HL				; $A388
			ADD HL,DE				; $A389
			ADD HL,BC				; $A38A
			LD ($A29A),HL				; $A38B
			POP DE				; $A38E
			LD A,E				; $A38F
			AND $7C				; $A390
			RRCA				; $A392
			RRCA				; $A393
			LD ($A412),A				; $A394
			LD C,D				; $A397
			LD HL,$2C77				; $A398
			LD ($A41C),HL				; $A39B
			LD ($A417),HL				; $A39E
			LD A,L				; $A3A1
			LD ($A421),A				; $A3A2
			LD A,E				; $A3A5
			CP $80				; $A3A6
			JR C,$A3C8				; $A3A8
			XOR A				; $A3AA
			LD ($A412),A				; $A3AB
			LD HL,$0000				; $A3AE
			LD ($A417),HL				; $A3B1
			LD A,E				; $A3B4
			CP $FC				; $A3B5
			JR NC,$A3C8				; $A3B7
			LD HL,$0000				; $A3B9
			LD ($A41C),HL				; $A3BC
			LD A,E				; $A3BF
			CP $F8				; $A3C0
			JR NC,$A3C8				; $A3C2
			XOR A				; $A3C4
			LD ($A421),A				; $A3C5
			EXX				; $A3C8
			POP DE				; $A3C9
			LD A,E				; $A3CA
			AND $7C				; $A3CB
			RRCA				; $A3CD
			RRCA				; $A3CE
			LD ($A42B),A				; $A3CF
			LD HL,$2C77				; $A3D2
			LD ($A435),HL				; $A3D5
			LD ($A430),HL				; $A3D8
			LD A,L				; $A3DB
			LD ($A43A),A				; $A3DC
			LD A,E				; $A3DF
			CP $80				; $A3E0
			JR C,$A402				; $A3E2
			XOR A				; $A3E4
			LD ($A42B),A				; $A3E5
			LD HL,$0000				; $A3E8
			LD ($A430),HL				; $A3EB
			LD A,E				; $A3EE
			CP $FC				; $A3EF
			JR NC,$A402				; $A3F1
			LD HL,$0000				; $A3F3
			LD ($A435),HL				; $A3F6
			LD A,E				; $A3F9
			CP $F8				; $A3FA
			JR NC,$A402				; $A3FC
			XOR A				; $A3FE
			LD ($A43A),A				; $A3FF
			LD C,D				; $A402
			POP DE				; $A403
			EXX				; $A404
			LD DE,($A29A)				; $A405
			LD B,$10				; $A409
			LD H,$64				; $A40B
			LD L,C				; $A40D
			LD A,(HL)				; $A40E
			DEC H				; $A40F
			LD H,(HL)				; $A410
			OR $00				; $A411
			LD L,A				; $A413
			INC C				; $A414
			LD A,(DE)				; $A415
			XOR (HL)				; $A416
			LD (HL),A				; $A417
			INC L				; $A418
			INC DE				; $A419
			LD A,(DE)				; $A41A
			XOR (HL)				; $A41B
			LD (HL),A				; $A41C
			INC L				; $A41D
			INC DE				; $A41E
			LD A,(DE)				; $A41F
			XOR (HL)				; $A420
			LD (HL),A				; $A421
			INC DE				; $A422
			EXX				; $A423
			LD H,$64				; $A424
			LD L,C				; $A426
			LD A,(HL)				; $A427
			DEC H				; $A428
			LD H,(HL)				; $A429
			OR $00				; $A42A
			LD L,A				; $A42C
			INC C				; $A42D
			LD A,(DE)				; $A42E
			XOR (HL)				; $A42F
			LD (HL),A				; $A430
			INC L				; $A431
			INC DE				; $A432
			LD A,(DE)				; $A433
			XOR (HL)				; $A434
			LD (HL),A				; $A435
			INC L				; $A436
			INC DE				; $A437
			LD A,(DE)				; $A438
			XOR (HL)				; $A439
			LD (HL),A				; $A43A
			INC DE				; $A43B
			EXX				; $A43C
			DJNZ $A40B				; $A43D
			LD HL,($A29A)				; $A43F
			POP DE				; $A442
			POP BC				; $A443
			POP AF				; $A444
			RET				; $A445

			PUSH AF				; $A446
			PUSH BC				; $A447
			PUSH DE				; $A448
			PUSH HL				; $A449
			LD B,$03				; $A44A
			LD A,E				; $A44C
			CP $80				; $A44D
			JR C,$A453				; $A44F
			LD E,$00				; $A451
			CALL $6799				; $A453
			PUSH HL				; $A456
			LD DE,$0300				; $A457
			ADD HL,DE				; $A45A
			EX DE,HL				; $A45B
			POP HL				; $A45C
			LD A,(DE)				; $A45D
			OR A				; $A45E
			JR NZ,$A462				; $A45F
			LD (HL),C				; $A461
			INC HL				; $A462
			INC DE				; $A463
			LD A,(DE)				; $A464
			OR A				; $A465
			JR NZ,$A469				; $A466
			LD (HL),C				; $A468
			INC HL				; $A469
			INC DE				; $A46A
			LD A,(DE)				; $A46B
			OR A				; $A46C
			JR NZ,$A470				; $A46D
			LD (HL),C				; $A46F
			LD DE,$001E				; $A470
			ADD HL,DE				; $A473
			DJNZ $A456				; $A474
			POP HL				; $A476
			POP DE				; $A477
			POP BC				; $A478
			POP AF				; $A479
			RET				; $A47A

			PUSH AF				; $A47B
			PUSH BC				; $A47C
			PUSH DE				; $A47D
			PUSH HL				; $A47E
			LD A,E				; $A47F
			CP $80				; $A480
			JR C,$A486				; $A482
			LD E,$00				; $A484
			LD A,$03				; $A486
			CALL $6799				; $A488
			EX AF,AF'				; $A48B
			PUSH HL				; $A48C
			LD DE,$0300				; $A48D
			ADD HL,DE				; $A490
			EX DE,HL				; $A491
			POP HL				; $A492
			LD A,(DE)				; $A493
			OR A				; $A494
			JR NZ,$A498				; $A495
			LD (HL),C				; $A497
			INC HL				; $A498
			INC DE				; $A499
			LD A,(DE)				; $A49A
			OR A				; $A49B
			JR NZ,$A49F				; $A49C
			LD (HL),B				; $A49E
			LD DE,$001F				; $A49F
			ADD HL,DE				; $A4A2
			EX AF,AF'				; $A4A3
			DEC A				; $A4A4
			JP NZ,$A48B				; $A4A5
			POP HL				; $A4A8
			POP DE				; $A4A9
			POP BC				; $A4AA
			POP AF				; $A4AB
			RET				; $A4AC

			PUSH AF				; $A4AD
			PUSH BC				; $A4AE
			PUSH DE				; $A4AF
			PUSH HL				; $A4B0
			LD B,$02				; $A4B1
			LD A,E				; $A4B3
			CP $80				; $A4B4
			JR C,$A4BA				; $A4B6
			LD E,$00				; $A4B8
			CALL $6799				; $A4BA
			PUSH HL				; $A4BD
			LD DE,$0300				; $A4BE
			ADD HL,DE				; $A4C1
			EX DE,HL				; $A4C2
			POP HL				; $A4C3
			LD A,(DE)				; $A4C4
			OR A				; $A4C5
			JR NZ,$A4C9				; $A4C6
			LD (HL),C				; $A4C8
			INC L				; $A4C9
			INC E				; $A4CA
			LD A,(DE)				; $A4CB
			OR A				; $A4CC
			JR NZ,$A4D0				; $A4CD
			LD (HL),C				; $A4CF
			LD DE,$001F				; $A4D0
			ADD HL,DE				; $A4D3
			DJNZ $A4BD				; $A4D4
			POP HL				; $A4D6
			POP DE				; $A4D7
			POP BC				; $A4D8
			POP AF				; $A4D9
			RET				; $A4DA

			PUSH AF				; $A4DB
			PUSH BC				; $A4DC
			PUSH DE				; $A4DD
			PUSH HL				; $A4DE
			PUSH BC				; $A4DF
			LD L,A				; $A4E0
			LD H,$00				; $A4E1
			ADD HL,HL				; $A4E3
			ADD HL,HL				; $A4E4
			ADD HL,HL				; $A4E5
			ADD HL,HL				; $A4E6
			ADD HL,HL				; $A4E7
			LD BC,$D479				; $A4E8
			ADD HL,BC				; $A4EB
			LD ($A29A),HL				; $A4EC
			LD A,E				; $A4EF
			AND $7C				; $A4F0
			RRCA				; $A4F2
			RRCA				; $A4F3
			LD ($A50F),A				; $A4F4
			LD C,D				; $A4F7
			EXX				; $A4F8
			POP DE				; $A4F9
			LD A,E				; $A4FA
			AND $7C				; $A4FB
			RRCA				; $A4FD
			RRCA				; $A4FE
			LD ($A523),A				; $A4FF
			LD C,D				; $A502
			POP DE				; $A503
			EXX				; $A504
			EX DE,HL				; $A505
			LD B,$10				; $A506
			LD H,$64				; $A508
			LD L,C				; $A50A
			LD A,(HL)				; $A50B
			DEC H				; $A50C
			LD H,(HL)				; $A50D
			OR $00				; $A50E
			LD L,A				; $A510
			INC C				; $A511
			LD A,(DE)				; $A512
			INC DE				; $A513
			XOR (HL)				; $A514
			LD (HL),A				; $A515
			INC L				; $A516
			LD A,(DE)				; $A517
			INC DE				; $A518
			XOR (HL)				; $A519
			LD (HL),A				; $A51A
			EXX				; $A51B
			LD H,$64				; $A51C
			LD L,C				; $A51E
			LD A,(HL)				; $A51F
			DEC H				; $A520
			LD H,(HL)				; $A521
			OR $00				; $A522
			LD L,A				; $A524
			INC C				; $A525
			LD A,(DE)				; $A526
			XOR (HL)				; $A527
			LD (HL),A				; $A528
			INC L				; $A529
			INC DE				; $A52A
			LD A,(DE)				; $A52B
			XOR (HL)				; $A52C
			LD (HL),A				; $A52D
			INC DE				; $A52E
			EXX				; $A52F
			DJNZ $A508				; $A530
			LD HL,($A29A)				; $A532
			POP DE				; $A535
			POP BC				; $A536
			POP AF				; $A537
			RET				; $A538

			PUSH BC				; $A539
			LD L,A				; $A53A
			LD H,$00				; $A53B
			ADD HL,HL				; $A53D
			ADD HL,HL				; $A53E
			ADD HL,HL				; $A53F
			ADD HL,HL				; $A540
			ADD HL,HL				; $A541
			LD BC,$D479				; $A542
			ADD HL,BC				; $A545
			POP BC				; $A546
			RET				; $A547

			CP $E9				; $A548
			RET NC				; $A54A
			PUSH AF				; $A54B
			PUSH BC				; $A54C
			PUSH DE				; $A54D
			PUSH HL				; $A54E
			LD L,A				; $A54F
			LD H,$00				; $A550
			ADD HL,HL				; $A552
			ADD HL,HL				; $A553
			ADD HL,HL				; $A554
			ADD HL,HL				; $A555
			ADD HL,HL				; $A556
			LD BC,$D479				; $A557
			ADD HL,BC				; $A55A
			LD A,E				; $A55B
			AND $7C				; $A55C
			RRCA				; $A55E
			RRCA				; $A55F
			LD ($A56E),A				; $A560
			LD C,D				; $A563
			EX DE,HL				; $A564
			LD B,$10				; $A565
			LD H,$64				; $A567
			LD L,C				; $A569
			LD A,(HL)				; $A56A
			DEC H				; $A56B
			LD H,(HL)				; $A56C
			OR $00				; $A56D
			LD L,A				; $A56F
			INC C				; $A570
			LD A,(DE)				; $A571
			INC DE				; $A572
			LD (HL),A				; $A573
			INC L				; $A574
			LD A,(DE)				; $A575
			INC DE				; $A576
			LD (HL),A				; $A577
			DJNZ $A567				; $A578
			POP HL				; $A57A
			POP DE				; $A57B
			POP BC				; $A57C
			POP AF				; $A57D
			RET				; $A57E

			CP $E9				; $A57F
			RET NC				; $A581
			PUSH AF				; $A582
			PUSH BC				; $A583
			PUSH DE				; $A584
			PUSH HL				; $A585
			LD L,A				; $A586
			LD H,$00				; $A587
			ADD HL,HL				; $A589
			ADD HL,HL				; $A58A
			LD BC,$E9B9				; $A58B
			ADD HL,BC				; $A58E
			LD B,H				; $A58F
			LD C,L				; $A590
			CALL $6799				; $A591
			LD A,(BC)				; $A594
			LD (HL),A				; $A595
			INC L				; $A596
			INC BC				; $A597
			LD A,(BC)				; $A598
			LD (HL),A				; $A599
			INC BC				; $A59A
			LD DE,$001F				; $A59B
			ADD HL,DE				; $A59E
			LD A,(BC)				; $A59F
			LD (HL),A				; $A5A0
			INC L				; $A5A1
			INC BC				; $A5A2
			LD A,(BC)				; $A5A3
			LD (HL),A				; $A5A4
			POP HL				; $A5A5
			POP DE				; $A5A6
			POP BC				; $A5A7
			POP AF				; $A5A8
			RET				; $A5A9

			defb $01                                            ; $A5AA .
			defb $02,$03,$04,$05,$01,$01,$15,$16				; $A5AB ........
			defb $17,$18,$16,$17,$15,$02,$01,$02				; $A5B3 ........
			defb $48,$FF,$05,$14,$13,$FF,$05,$00				; $A5BB H.......
			defb $15,$01,$02,$03,$15,$00,$1D,$00				; $A5C3 ........
			defb $1F,$FF,$05,$00,$10,$14,$4B,$01				; $A5CB ......K.
			defb $01,$04,$15,$FF,$09,$00,$15,$02				; $A5D3 ........
			defb $01,$01,$01,$05,$15,$FF,$09,$00				; $A5DB ........
			defb $15,$01,$03,$01,$02,$01,$15,$FF				; $A5E3 ........
			defb $09,$00,$15,$04,$01,$01,$01,$02				; $A5EB ........
			defb $15,$FF,$09,$00,$12,$14,$49,$01				; $A5F3 ......I.
			defb $02,$03,$15,$FF,$0A,$00,$1D,$15				; $A5FB ........
			defb $03,$04,$04,$15,$00,$00,$96,$0D				; $A603 ........
			defb $80,$81,$82,$00,$0F,$0E,$0D,$4A				; $A60B .......J
			defb $14,$14,$01,$15,$00,$00,$15,$01				; $A613 ........
			defb $02,$03,$04,$05,$01,$04,$03,$01				; $A61B ........
			defb $01,$02,$02,$03,$04,$05,$02,$03				; $A623 ........
			defb $04,$05,$15,$01,$01,$02,$03,$04				; $A62B ........
			defb $05,$02,$FF,$08,$14,$13,$34,$35				; $A633 ......45
			defb $34,$35,$34,$35,$34,$7C,$00,$4E				; $A63B 45454|.N
			defb $4F,$FF,$0C,$00,$7E,$00,$50,$51				; $A643 O...~.PQ
			defb $FF,$0C,$00,$7E,$00,$52,$53,$FF				; $A64B ...~.RS.
			defb $0C,$00,$7D,$FF,$0F,$00,$7C,$FF				; $A653 ..}...|.
			defb $0D,$00,$27,$28,$7E,$FF,$07,$00				; $A65B ..'(~...
			defb $23,$24,$FF,$04,$00,$29,$2A,$7D				; $A663 #$...)*}
			defb $FF,$07,$00,$25,$26,$00,$00,$00				; $A66B ...%&...
			defb $10,$14,$14,$7C,$FF,$05,$00,$79				; $A673 ...|...y
			defb $FF,$05,$78,$79,$15,$01,$01,$01				; $A67B ..xy....
			defb $02,$03,$01,$02,$03,$04,$05,$01				; $A683 ........
			defb $03,$04,$05,$15,$01,$02,$03,$34				; $A68B .......4
			defb $35,$36,$35,$36,$37,$36,$35,$34				; $A693 56567654
			defb $35,$35,$34,$15,$04,$05,$01,$FF				; $A69B 554.....
			defb $0C,$00,$12,$14,$14,$14,$FF,$0D				; $A6A3 ........
			defb $00,$30,$31,$FF,$0E,$00,$32,$33				; $A6AB .01...23
			defb $FF,$1D,$00,$FF,$04,$EC,$FF,$10				; $A6B3 ........
			defb $00,$FF,$06,$14,$11,$FF,$09,$00				; $A6BB ........
			defb $01,$02,$03,$03,$01,$02,$15,$0D				; $A6C3 ........
			defb $0E,$0F,$0D,$0E,$0F,$0D,$0E,$0E				; $A6CB ........
			defb $01,$01,$04,$01,$15,$19,$1A,$FF				; $A6D3 ........
			defb $09,$1B,$02,$03,$02,$03,$15,$00				; $A6DB ........
			defb $00,$3C,$00,$3C,$00,$3C,$FF,$04				; $A6E3 .<.<.<..
			defb $FA,$FF,$04,$14,$13,$00,$00,$3B				; $A6EB .......;
			defb $F1,$3B,$00,$3D,$FF,$0B,$00,$3D				; $A6F3 .;.=...=
			defb $00,$3B,$F6,$FF,$0E,$00,$3D,$00				; $A6FB .;....=.
			defb $3C,$FF,$0B,$00,$3C,$00,$00,$00				; $A703 <...<...
			defb $3B,$00,$10,$14,$14,$FF,$07,$00				; $A70B ;.......
			defb $3B,$00,$3C,$00,$3B,$00,$15,$01				; $A713 ;.<.;...
			defb $01,$FF,$07,$00,$3D,$00,$3D,$00				; $A71B ....=.=.
			defb $3D,$00,$15,$02,$03,$FF,$05,$00				; $A723 =.......
			defb $10,$FF,$07,$14,$4B,$04,$05,$0C				; $A72B ....K...
			defb $0D,$0E,$0D,$0E,$15,$01,$02,$03				; $A733 ........
			defb $04,$05,$01,$03,$04,$01,$02,$FF				; $A73B ........
			defb $0A,$1B,$1C,$19,$15,$01,$02,$01				; $A743 ........
			defb $00,$00,$00,$2B,$2C,$00,$2B,$2C				; $A74B ...+,.+,
			defb $FF,$04,$00,$12,$14,$14,$49,$FF				; $A753 ......I.
			defb $04,$00,$2D,$00,$00,$2D,$FF,$07				; $A75B ..-..-..
			defb $00,$15,$FF,$0E,$00,$2E,$15,$FF				; $A763 ........
			defb $0A,$00,$E9,$00,$EA,$2F,$2E,$15				; $A76B ...../..
			defb $14,$14,$14,$11,$FF,$05,$00,$2E				; $A773 ........
			defb $2F,$2E,$2F,$2E,$2F,$15,$01,$02				; $A77B /././...
			defb $03,$15,$FF,$04,$00,$2E,$2F,$2E				; $A783 ....../.
			defb $2F,$2E,$2F,$2E,$15,$01,$03,$04				; $A78B /./.....
			defb $15,$00,$00,$2F,$2E,$2F,$2E,$2F				; $A793 .../././
			defb $2E,$2F,$2E,$2F,$15,$01,$02,$02				; $A79B ././....
			defb $4A,$FF,$06,$14,$49,$2F,$2E,$48				; $A7A3 J...I/.H
			defb $14,$4B,$01,$02,$03,$04,$01,$02				; $A7AB .K......
			defb $03,$01,$01,$02,$15,$2E,$2F,$15				; $A7B3 ....../.
			defb $01,$02,$FF,$A0,$00,$01,$15,$00				; $A7BB ........
			defb $00,$15,$02,$03,$04,$01,$02,$02				; $A7C3 ........
			defb $01,$02,$01,$01,$02,$01,$15,$00				; $A7CB ........
			defb $00,$12,$FF,$08,$14,$49,$03,$02				; $A7D3 .....I..
			defb $02,$15,$FF,$0B,$00,$15,$02,$03				; $A7DB ........
			defb $03,$15,$FF,$09,$00,$10,$14,$4B				; $A7E3 .......K
			defb $02,$03,$04,$15,$FF,$09,$00,$15				; $A7EB ........
			defb $01,$01,$01,$02,$04,$15,$FF,$09				; $A7F3 ........
			defb $00,$15,$01,$02,$03,$04,$01,$15				; $A7FB ........
			defb $FF,$09,$00,$12,$14,$14,$49,$01				; $A803 ......I.
			defb $02,$15,$EB,$FF,$0B,$00,$15,$01				; $A80B ........
			defb $03,$4A,$14,$11,$FF,$08,$00,$10				; $A813 .J......
			defb $14,$4B,$01,$04,$01,$02,$15,$00				; $A81B .K......
			defb $00,$ED,$ED,$FF,$04,$00,$15,$02				; $A823 ........
			defb $01,$02,$7C,$FF,$05,$00,$79,$FF				; $A82B ..|...y.
			defb $05,$78,$79,$15,$03,$01,$7D,$FF				; $A833 .xy...}.
			defb $06,$00,$3C,$00,$3C,$F6,$3C,$00				; $A83B ..<.<.<.
			defb $12,$14,$14,$7D,$FF,$06,$00,$3B				; $A843 ...}...;
			defb $F6,$3D,$00,$3B,$FF,$04,$00,$7E				; $A84B .=.;...~
			defb $FF,$06,$00,$3B,$00,$00,$00,$3D				; $A853 ...;...=
			defb $FF,$04,$00,$7C,$FF,$06,$00,$3D				; $A85B ...|...=
			defb $00,$3C,$FF,$06,$00,$7D,$FF,$08				; $A863 .<...}..
			defb $00,$3B,$00,$3C,$FF,$04,$00,$7E				; $A86B .;.<...~
			defb $FF,$06,$00,$3C,$00,$3B,$00,$3B				; $A873 ...<.;.;
			defb $00,$10,$14,$14,$7D,$FF,$05,$00				; $A87B ....}...
			defb $E9,$3D,$00,$3D,$00,$3D,$00,$15				; $A883 .=.=.=..
			defb $03,$04,$7C,$0C,$80,$81,$82,$10				; $A88B ..|.....
			defb $FF,$07,$14,$4B,$02,$01,$7F,$01				; $A893 ...K....
			defb $02,$03,$04,$15,$01,$02,$03,$04				; $A89B ........
			defb $01,$02,$03,$04,$01,$02,$01,$02				; $A8A3 ........
			defb $03,$04,$01,$02,$03,$04,$01,$02				; $A8AB ........
			defb $03,$04,$02,$01,$03,$01,$FF,$10				; $A8B3 ........
			defb $14,$FF,$0A,$00,$47,$FF,$04,$00				; $A8BB ....G...
			defb $7C,$EE,$EE,$EE,$FF,$0C,$00,$7D				; $A8C3 |......}
			defb $FF,$0F,$00,$7E,$FF,$0F,$00,$7C				; $A8CB ...~...|
			defb $14,$14,$11,$FF,$0C,$00,$7D,$01				; $A8D3 ......}.
			defb $03,$15,$00,$00,$00,$E9,$00,$E9				; $A8DB ........
			defb $FF,$06,$00,$7D,$02,$03,$4A,$FF				; $A8E3 ...}..J.
			defb $07,$14,$11,$00,$00,$00,$10,$14				; $A8EB ........
			defb $01,$02,$03,$04,$05,$01,$02,$03				; $A8F3 ........
			defb $04,$05,$15,$00,$00,$00,$15,$01				; $A8FB ........
			defb $01,$02,$03,$04,$01,$15,$79,$FF				; $A903 ......y.
			defb $07,$78,$79,$11,$01,$48,$14,$14				; $A90B .xy..H..
			defb $14,$13,$00,$00,$30,$31,$FF,$05				; $A913 ....01..
			defb $00,$15,$02,$15,$FF,$06,$00,$32				; $A91B .......2
			defb $33,$FF,$05,$00,$15,$03,$15,$FF				; $A923 3.......
			defb $0D,$00,$15,$14,$13,$FF,$08,$00				; $A92B ........
			defb $85,$83,$84,$86,$00,$15,$7C,$FF				; $A933 ......|.
			defb $09,$00,$87,$00,$00,$87,$00,$15				; $A93B ........
			defb $7C,$FF,$09,$00,$87,$00,$00,$87				; $A943 |.......
			defb $00,$15,$7D,$FF,$09,$00,$87,$00				; $A94B ..}.....
			defb $00,$87,$00,$15,$FF,$05,$14,$11				; $A953 ........
			defb $FF,$04,$00,$88,$89,$8A,$88,$00				; $A95B ........
			defb $15,$01,$02,$03,$04,$05,$15,$00				; $A963 ........
			defb $00,$10,$FF,$06,$14,$4B,$01,$02				; $A96B .....K..
			defb $15,$04,$01,$02,$03,$15,$02,$03				; $A973 ........
			defb $15,$00,$00,$15,$01,$02,$01,$02				; $A97B ........
			defb $15,$34,$35,$36,$34,$12,$14,$14				; $A983 .4564...
			defb $13,$00,$00,$12,$14,$49,$48,$14				; $A98B .....IH.
			defb $13,$FF,$0C,$00,$15,$15,$FF,$0E				; $A993 ........
			defb $00,$15,$15,$FF,$0E,$00,$15,$15				; $A99B ........
			defb $FF,$0E,$00,$15,$15,$FF,$0C,$00				; $A9A3 ........
			defb $27,$28,$15,$15,$FF,$0C,$00,$29				; $A9AB '(.....)
			defb $2A,$15,$15,$FF,$0B,$00,$10,$14				; $A9B3 *.......
			defb $14,$4B,$15,$FF,$05,$00,$ED,$ED				; $A9BB .K......
			defb $ED,$00,$00,$00,$15,$01,$02,$03				; $A9C3 ........
			defb $FF,$A0,$00,$01,$02,$03,$15,$FF				; $A9CB ........
			defb $08,$00,$15,$01,$03,$02,$02,$01				; $A9D3 ........
			defb $02,$15,$FF,$08,$00,$15,$01,$02				; $A9DB ........
			defb $03,$01,$48,$14,$13,$FF,$08,$00				; $A9E3 ..H.....
			defb $12,$14,$49,$01,$02,$15,$FF,$0C				; $A9EB ..I.....
			defb $00,$15,$02,$03,$15,$20,$FF,$0B				; $A9F3 ..... ..
			defb $00,$15,$02,$04,$4A,$14,$11,$FF				; $A9FB ....J...
			defb $0A,$00,$12,$14,$01,$02,$03,$15				; $AA03 ........
			defb $FF,$0C,$00,$04,$01,$03,$15,$FF				; $AA0B ........
			defb $07,$00,$27,$28,$00,$00,$00,$03				; $AA13 ..'(....
			defb $04,$01,$15,$00,$00,$22,$FF,$04				; $AA1B ....."..
			defb $00,$29,$2A,$00,$00,$00,$14,$14				; $AA23 .)*.....
			defb $14,$4B,$19,$1A,$FF,$0A,$1B,$01				; $AA2B .K......
			defb $02,$04,$03,$01,$15,$04,$01,$02				; $AA33 ........
			defb $03,$04,$05,$01,$02,$03,$04,$05				; $AA3B ........
			defb $04,$03,$03,$02,$15,$34,$35,$36				; $AA43 .....456
			defb $37,$35,$36,$35,$37,$35,$34,$02				; $AA4B 7565754.
			defb $03,$04,$05,$01,$15,$FF,$0A,$00				; $AA53 ........
			defb $03,$04,$01,$01,$02,$15,$FF,$0A				; $AA5B ........
			defb $00,$03,$04,$01,$02,$04,$15,$FF				; $AA63 ........
			defb $0A,$00,$FF,$05,$14,$13,$FF,$15				; $AA6B ........
			defb $00,$94,$95,$FF,$04,$00,$91,$00				; $AA73 ........
			defb $91,$00,$91,$FF,$05,$00,$3C,$00				; $AA7B ......<.
			defb $46,$46,$00,$00,$92,$93,$92,$93				; $AA83 FF......
			defb $92,$FF,$04,$00,$0E,$3D,$0C,$4C				; $AA8B .....=.L
			defb $4D,$0D,$FF,$07,$1B,$1C,$19,$0C				; $AA93 M.......
			defb $02,$02,$03,$04,$05,$01,$01,$02				; $AA9B ........
			defb $15,$04,$15,$FF,$05,$78,$79,$00				; $AAA3 .....xy.
			defb $00,$00,$15,$01,$34,$34,$12,$14				; $AAAB ....44..
			defb $13,$00,$3C,$00,$3C,$FF,$05,$00				; $AAB3 ..<.<...
			defb $15,$02,$FF,$06,$00,$3D,$F1,$3B				; $AABB .....=.;
			defb $FF,$05,$00,$15,$04,$FF,$08,$00				; $AAC3 ........
			defb $3B,$FF,$05,$00,$15,$02,$FF,$06				; $AACB ;.......
			defb $00,$3C,$00,$3D,$FF,$05,$00,$15				; $AAD3 .<.=....
			defb $03,$FF,$06,$00,$3B,$FF,$07,$00				; $AADB ....;...
			defb $15,$01,$FF,$06,$00,$3B,$00,$3C				; $AAE3 .....;.<
			defb $FF,$05,$00,$15,$04,$FF,$06,$00				; $AAEB ........
			defb $3B,$00,$3B,$FF,$05,$00,$15,$03				; $AAF3 ;.;.....
			defb $0C,$0D,$0C,$00,$00,$00,$3D,$00				; $AAFB ......=.
			defb $3D,$FF,$05,$00,$15,$02,$02,$04				; $AB03 =.......
			defb $05,$0E,$19,$1A,$1B,$1B,$1B,$1C				; $AB0B ........
			defb $19,$16,$17,$18,$15,$01,$01,$04				; $AB13 ........
			defb $03,$02,$01,$15,$00,$00,$15,$01				; $AB1B ........
			defb $02,$03,$04,$04,$04,$01,$02,$48				; $AB23 .......H
			defb $14,$14,$14,$4B,$00,$00,$4A,$14				; $AB2B ...K..J.
			defb $14,$14,$49,$03,$03,$02,$03,$15				; $AB33 ..I.....
			defb $FF,$0A,$00,$15,$02,$02,$02,$04				; $AB3B ........
			defb $15,$FF,$0A,$00,$15,$01,$01,$03				; $AB43 ........
			defb $01,$15,$FF,$08,$00,$2E,$00,$12				; $AB4B ........
			defb $14,$14,$14,$02,$15,$FF,$08,$00				; $AB53 ........
			defb $FA,$FF,$05,$00,$03,$15,$00,$46				; $AB5B .......F
			defb $46,$00,$00,$46,$46,$00,$00,$00				; $AB63 F..FF...
			defb $10,$14,$14,$14,$04,$15,$00,$4C				; $AB6B .......L
			defb $4D,$00,$46,$4C,$4D,$00,$00,$00				; $AB73 M.FLM...
			defb $15,$01,$02,$03,$04,$4A,$FF,$0A				; $AB7B .....J..
			defb $14,$4B,$04,$01,$02,$01,$02,$03				; $AB83 .K......
			defb $04,$01,$02,$04,$04,$01,$02,$03				; $AB8B ........
			defb $04,$01,$02,$03,$04,$15,$FF,$0B				; $AB93 ........
			defb $00,$15,$01,$02,$03,$15,$8D,$FF				; $AB9B ........
			defb $0A,$00,$15,$01,$02,$03,$15,$90				; $ABA3 ........
			defb $FF,$0A,$00,$12,$14,$49,$03,$15				; $ABAB .....I..
			defb $8D,$FF,$0B,$00,$FD,$15,$01,$13				; $ABB3 ........
			defb $8F,$8D,$FF,$0A,$00,$FD,$15,$02				; $ABBB ........
			defb $00,$8D,$8F,$8D,$FF,$09,$00,$FD				; $ABC3 ........
			defb $15,$03,$11,$8F,$8D,$8F,$8D,$00				; $ABCB ........
			defb $00,$46,$46,$00,$23,$24,$00,$FD				; $ABD3 .FF.#$..
			defb $15,$04,$15,$8D,$8F,$8D,$8F,$8D				; $ABDB ........
			defb $8D,$4C,$4D,$00,$25,$26,$00,$FD				; $ABE3 .LM.%&..
			defb $15,$04,$4A,$FF,$0D,$14,$4B,$04				; $ABEB ..J...K.
			defb $01,$02,$02,$03,$04,$01,$02,$03				; $ABF3 ........
			defb $04,$01,$03,$04,$01,$02,$03,$04				; $ABFB ........
			defb $FF,$A0,$00,$01,$02,$48,$FF,$0D				; $AC03 .....H..
			defb $14,$04,$03,$15,$00,$00,$3C,$00				; $AC0B ......<.
			defb $3C,$00,$3C,$00,$3C,$FF,$04,$00				; $AC13 <.<.<...
			defb $02,$01,$15,$00,$00,$3D,$00,$3B				; $AC1B .....=.;
			defb $00,$3D,$00,$3B,$00,$10,$14,$14				; $AC23 .=.;....
			defb $04,$01,$15,$FF,$04,$00,$3B,$00				; $AC2B ......;.
			defb $00,$00,$3B,$00,$15,$01,$02,$03				; $AC33 ..;.....
			defb $02,$15,$00,$00,$3C,$F2,$3B,$F6				; $AC3B ....<.;.
			defb $3C,$F2,$3B,$00,$15,$03,$04,$04				; $AC43 <.;.....
			defb $01,$15,$00,$00,$3B,$00,$3D,$00				; $AC4B ....;.=.
			defb $3B,$00,$3D,$00,$15,$01,$02,$04				; $AC53 ;.=.....
			defb $02,$15,$00,$00,$3B,$00,$00,$00				; $AC5B ....;...
			defb $3B,$00,$00,$00,$15,$03,$04,$03				; $AC63 ;.......
			defb $02,$15,$00,$00,$3B,$00,$3C,$00				; $AC6B ....;.<.
			defb $3B,$00,$3C,$00,$15,$01,$02,$04				; $AC73 ;.<.....
			defb $01,$15,$00,$00,$3D,$00,$3B,$00				; $AC7B ....=.;.
			defb $3D,$00,$3B,$00,$15,$03,$04,$01				; $AC83 =.;.....
			defb $02,$15,$00,$10,$FF,$08,$14,$4B				; $AC8B .......K
			defb $01,$02,$FF,$0D,$14,$49,$01,$02				; $AC93 .....I..
			defb $00,$00,$47,$47,$47,$FF,$05,$00				; $AC9B ..GGG...
			defb $30,$31,$00,$15,$03,$04,$14,$11				; $ACA3 01......
			defb $FF,$08,$00,$32,$33,$00,$15,$01				; $ACAB ...23...
			defb $02,$01,$15,$FF,$0B,$00,$15,$04				; $ACB3 ........
			defb $03,$02,$15,$FF,$0B,$00,$15,$05				; $ACBB ........
			defb $01,$03,$15,$FF,$05,$00,$A0,$A1				; $ACC3 ........
			defb $FF,$04,$00,$15,$02,$03,$04,$15				; $ACCB ........
			defb $FF,$05,$00,$A2,$A3,$00,$23,$24				; $ACD3 ......#$
			defb $00,$15,$05,$04,$01,$15,$FF,$05				; $ACDB ........
			defb $00,$A4,$A5,$00,$25,$26,$00,$15				; $ACE3 ....%&..
			defb $01,$02,$02,$15,$FF,$04,$00,$10				; $ACEB ........
			defb $FF,$06,$14,$4B,$03,$03,$03,$15				; $ACF3 ...K....
			defb $FF,$04,$00,$15,$01,$02,$03,$04				; $ACFB ........
			defb $05,$01,$02,$01,$02,$01,$02,$03				; $AD03 ........
			defb $04,$03,$02,$01,$04,$03,$02,$01				; $AD0B ........
			defb $05,$04,$03,$01,$02,$01,$01,$02				; $AD13 ........
			defb $03,$48,$FF,$0B,$14,$02,$01,$03				; $AD1B .H......
			defb $02,$15,$00,$4E,$4F,$FF,$08,$00				; $AD23 ...NO...
			defb $03,$48,$14,$14,$13,$00,$50,$51				; $AD2B .H....PQ
			defb $FF,$08,$00,$04,$15,$FF,$04,$00				; $AD33 ........
			defb $52,$53,$FF,$08,$00,$01,$15,$FF				; $AD3B RS......
			defb $0E,$00,$02,$15,$00,$00,$8D,$FF				; $AD43 ........
			defb $05,$00,$8D,$E9,$8D,$00,$00,$00				; $AD4B ........
			defb $03,$15,$00,$8D,$8F,$8D,$00,$8D				; $AD53 ........
			defb $00,$8D,$8F,$8D,$8F,$8D,$00,$00				; $AD5B ........
			defb $04,$4A,$14,$14,$49,$8F,$8D,$8F				; $AD63 .J..I...
			defb $8D,$8F,$8D,$8F,$48,$14,$14,$14				; $AD6B ....H...
			defb $01,$02,$03,$01,$15,$8D,$8F,$8D				; $AD73 ........
			defb $8F,$8D,$8F,$8D,$15,$01,$02,$03				; $AD7B ........
			defb $01,$02,$03,$04,$05,$02,$03,$04				; $AD83 ........
			defb $05,$01,$02,$03,$04,$05,$02,$01				; $AD8B ........
			defb $FF,$06,$14,$9A,$9B,$9C,$FF,$05				; $AD93 ........
			defb $14,$49,$01,$FF,$07,$00,$47,$FF				; $AD9B .I....G.
			defb $06,$00,$15,$02,$FF,$0E,$00,$15				; $ADA3 ........
			defb $03,$FF,$0C,$00,$0B,$00,$15,$04				; $ADAB ........
			defb $FF,$06,$00,$A0,$A1,$FF,$04,$00				; $ADB3 ........
			defb $0A,$00,$15,$05,$FF,$04,$00,$46				; $ADBB .......F
			defb $46,$A2,$A3,$46,$00,$00,$00,$09				; $ADC3 F..F....
			defb $00,$15,$02,$00,$00,$E9,$00,$4E				; $ADCB .......N
			defb $4F,$A4,$A5,$4E,$4F,$00,$06,$07				; $ADD3 O..NO...
			defb $08,$15,$03,$14,$14,$14,$9A,$FF				; $ADDB ........
			defb $06,$9B,$9C,$14,$14,$14,$4B,$04				; $ADE3 ......K.
			defb $05,$02,$03,$04,$05,$01,$02,$03				; $ADEB ........
			defb $01,$02,$02,$03,$04,$05,$02,$05				; $ADF3 ........
			defb $04,$02,$03,$04,$02,$01,$03,$04				; $ADFB ........
			defb $01,$02,$05,$04,$01,$02,$03,$01				; $AE03 ........
			defb $03,$01,$48,$FF,$0D,$14,$01,$02				; $AE0B ..H.....
			defb $15,$FF,$07,$00,$FF,$05,$F8,$00				; $AE13 ........
			defb $02,$03,$15,$00,$00,$10,$14,$14				; $AE1B ........
			defb $2E,$2F,$2E,$FF,$05,$14,$05,$01				; $AE23 ./......
			defb $15,$00,$00,$15,$FE,$FF,$05,$00				; $AE2B ........
			defb $FC,$19,$1A,$1B,$03,$04,$15,$00				; $AE33 ........
			defb $00,$12,$14,$14,$2F,$2E,$2F,$14				; $AE3B ...././.
			defb $14,$14,$49,$02,$01,$02,$15,$FF				; $AE43 ..I.....
			defb $0B,$00,$15,$04,$03,$04,$15,$00				; $AE4B ........
			defb $00,$00,$93,$00,$93,$00,$93,$00				; $AE53 ........
			defb $00,$00,$15,$03,$01,$02,$4A,$FF				; $AE5B ......J.
			defb $09,$14,$11,$00,$15,$02,$01,$02				; $AE63 ........
			defb $03,$05,$01,$02,$03,$04,$01,$02				; $AE6B ........
			defb $03,$04,$15,$00,$15,$04,$01,$15				; $AE73 ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $AE7B ........
			defb $01,$03,$04,$01,$02,$03,$14,$13				; $AE83 ........
			defb $34,$35,$36,$37,$35,$36,$37,$35				; $AE8B 45675675
			defb $34,$35,$34,$36,$35,$34,$FF,$0E				; $AE93 454654..
			defb $00,$48,$14,$14,$11,$FF,$0C,$00				; $AE9B .H......
			defb $15,$01,$01,$15,$FF,$0C,$00,$15				; $AEA3 ........
			defb $02,$02,$15,$FF,$0C,$00,$15,$03				; $AEAB ........
			defb $03,$15,$FF,$0C,$00,$15,$04,$05				; $AEB3 ........
			defb $15,$FF,$0C,$00,$15,$05,$04,$15				; $AEBB ........
			defb $FF,$0C,$00,$15,$01,$01,$15,$ED				; $AEC3 ........
			defb $ED,$ED,$FF,$09,$00,$15,$02,$01				; $AECB ........
			defb $02,$9D,$00,$15,$01,$02,$01,$02				; $AED3 ........
			defb $04,$01,$02,$03,$04,$01,$03,$03				; $AEDB ........
			defb $04,$9F,$00,$15,$01,$02,$03,$04				; $AEE3 ........
			defb $01,$02,$03,$04,$01,$02,$02,$78				; $AEEB .......x
			defb $78,$79,$00,$12,$14,$14,$14,$9A				; $AEF3 xy......
			defb $9B,$9C,$14,$14,$14,$49,$01,$01				; $AEFB .....I..
			defb $79,$FF,$0C,$00,$15,$04,$02,$9D				; $AF03 y.......
			defb $FF,$0C,$00,$9D,$03,$03,$9E,$FF				; $AF0B ........
			defb $0C,$00,$9E,$01,$04,$9E,$FF,$0C				; $AF13 ........
			defb $00,$9F,$01,$04,$9F,$FF,$0C,$00				; $AF1B ........
			defb $15,$04,$01,$79,$78,$79,$FF,$08				; $AF23 ...yxy..
			defb $00,$10,$14,$4B,$04,$02,$03,$04				; $AF2B ...K....
			defb $9D,$00,$00,$ED,$ED,$ED,$00,$00				; $AF33 ........
			defb $00,$15,$01,$02,$03,$04,$15,$00				; $AF3B ........
			defb $00,$00,$F9,$15,$01,$02,$03,$04				; $AF43 ........
			defb $01,$02,$03,$04,$01,$01,$9D,$00				; $AF4B ........
			defb $00,$00,$F9,$15,$02,$03,$04,$01				; $AF53 ........
			defb $02,$03,$04,$01,$02,$02,$9E,$00				; $AF5B ........
			defb $00,$00,$F9,$15,$03,$04,$01,$02				; $AF63 ........
			defb $03,$04,$02,$02,$03,$03,$9E,$FF				; $AF6B ........
			defb $04,$00,$12,$14,$9A,$9B,$9B,$9C				; $AF73 ........
			defb $FF,$04,$14,$04,$9E,$FF,$0E,$00				; $AF7B ........
			defb $01,$9E,$FF,$06,$00,$91,$00,$91				; $AF83 ........
			defb $00,$10,$14,$14,$14,$02,$9E,$00				; $AF8B ........
			defb $A0,$A1,$00,$00,$00,$92,$93,$92				; $AF93 ........
			defb $93,$15,$04,$04,$04,$03,$9F,$00				; $AF9B ........
			defb $A2,$A3,$10,$FF,$06,$14,$4B,$04				; $AFA3 ......K.
			defb $04,$04,$01,$15,$EB,$A4,$A5,$15				; $AFAB ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $AFB3 ........
			defb $04,$04,$02,$4A,$14,$14,$14,$4B				; $AFBB ...J...K
			defb $01,$02,$03,$04,$04,$04,$01,$04				; $AFC3 ........
			defb $04,$04,$01,$02,$03,$04,$15,$FF				; $AFCB ........
			defb $07,$00,$15,$01,$02,$03,$01,$48				; $AFD3 .......H
			defb $14,$14,$13,$FF,$07,$00,$15,$01				; $AFDB ........
			defb $02,$01,$02,$15,$FF,$0A,$00,$12				; $AFE3 ........
			defb $14,$49,$02,$14,$13,$FF,$0C,$00				; $AFEB .I......
			defb $9E,$03,$EE,$EE,$FF,$0C,$00,$9E				; $AFF3 ........
			defb $04,$14,$11,$FF,$0C,$00,$9E,$05				; $AFFB ........
			defb $01,$15,$FF,$0A,$00,$10,$14,$4B				; $B003 .......K
			defb $02,$02,$15,$00,$EA,$FF,$08,$00				; $B00B ........
			defb $15,$02,$01,$01,$03,$4A,$9A,$9B				; $B013 .....J..
			defb $9C,$11,$0C,$0D,$80,$81,$82,$0E				; $B01B ........
			defb $15,$02,$01,$05,$01,$02,$03,$04				; $B023 ........
			defb $05,$15,$02,$03,$04,$05,$01,$02				; $B02B ........
			defb $15,$04,$05,$01,$03,$02,$01,$04				; $B033 ........
			defb $03,$02,$01,$04,$03,$02,$01,$03				; $B03B ........
			defb $04,$02,$01,$01,$04,$48,$14,$9A				; $B043 .....H..
			defb $9B,$9C,$14,$9A,$9B,$9C,$FF,$06				; $B04B ........
			defb $14,$01,$15,$FF,$09,$00,$2F,$2E				; $B053 ....../.
			defb $2F,$2E,$00,$02,$15,$00,$97,$98				; $B05B /.......
			defb $FF,$05,$00,$2F,$2E,$48,$14,$14				; $B063 .../.H..
			defb $14,$03,$15,$00,$00,$3C,$FF,$04				; $B06B .....<..
			defb $00,$2F,$2E,$2F,$15,$03,$05,$01				; $B073 ././....
			defb $04,$15,$00,$00,$3B,$00,$00,$00				; $B07B ....;...
			defb $2F,$2E,$2F,$2E,$15,$02,$01,$04				; $B083 /./.....
			defb $04,$15,$E9,$EA,$3D,$00,$2E,$2F				; $B08B ....=../
			defb $2E,$2F,$2E,$2F,$4A,$14,$49,$02				; $B093 ././J.I.
			defb $01,$4A,$FF,$04,$14,$49,$2E,$2F				; $B09B .J...I./
			defb $2E,$2F,$2E,$2F,$2E,$15,$03,$02				; $B0A3 ././....
			defb $04,$01,$02,$03,$04,$15,$00,$2E				; $B0AB ........
			defb $2F,$2E,$2F,$2E,$00,$15,$03,$03				; $B0B3 /./.....
			defb $01,$02,$03,$04,$05,$9D,$FF,$07				; $B0BB ........
			defb $00,$15,$04,$03,$02,$04,$05,$01				; $B0C3 ........
			defb $02,$03,$04,$05,$01,$02,$03,$15				; $B0CB ........
			defb $00,$15,$03,$FF,$0C,$14,$13,$00				; $B0D3 ........
			defb $15,$03,$FF,$0E,$00,$15,$02,$14				; $B0DB ........
			defb $14,$11,$2E,$10,$FF,$04,$14,$11				; $B0E3 ........
			defb $FF,$04,$00,$15,$01,$02,$03,$15				; $B0EB ........
			defb $2F,$15,$01,$02,$03,$04,$15,$00				; $B0F3 /.......
			defb $48,$14,$14,$4B,$04,$05,$01,$15				; $B0FB H..K....
			defb $2E,$15,$04,$02,$05,$01,$15,$00				; $B103 ........
			defb $15,$01,$05,$01,$05,$48,$14,$13				; $B10B .....H..
			defb $2F,$12,$FF,$04,$14,$13,$00,$15				; $B113 /.......
			defb $01,$02,$02,$04,$15,$00,$00,$00				; $B11B ........
			defb $FF,$06,$FA,$00,$15,$03,$04,$03				; $B123 ........
			defb $03,$4A,$FF,$0A,$14,$4B,$05,$01				; $B12B .J...K..
			defb $04,$02,$01,$02,$03,$05,$01,$02				; $B133 ........
			defb $03,$04,$05,$02,$03,$04,$02,$03				; $B13B ........
			defb $05,$01,$02,$15,$FF,$0C,$00,$15				; $B143 ........
			defb $03,$05,$15,$8D,$8F,$8D,$8F,$8D				; $B14B ........
			defb $8F,$8D,$8F,$8D,$8F,$8D,$8F,$15				; $B153 ........
			defb $04,$02,$15,$8E,$8D,$8F,$8D,$8F				; $B15B ........
			defb $8D,$8F,$8D,$8F,$8D,$8E,$8D,$15				; $B163 ........
			defb $04,$03,$15,$00,$00,$8D,$00,$8D				; $B16B ........
			defb $00,$8D,$00,$8D,$00,$00,$00,$15				; $B173 ........
			defb $01,$04,$15,$FF,$0A,$00,$27,$28				; $B17B ......'(
			defb $15,$02,$05,$15,$FF,$0A,$00,$29				; $B183 .......)
			defb $2A,$15,$03,$03,$15,$FF,$0A,$00				; $B18B *.......
			defb $4E,$4F,$15,$04,$02,$15,$FF,$09				; $B193 NO......
			defb $00,$10,$14,$14,$4B,$01,$04,$15				; $B19B ....K...
			defb $FF,$09,$00,$15,$01,$02,$03,$02				; $B1A3 ........
			defb $05,$15,$ED,$ED,$ED,$FF,$06,$00				; $B1AB ........
			defb $15,$03,$04,$01,$02,$01,$02,$03				; $B1B3 ........
			defb $15,$FF,$08,$00,$15,$01,$02,$03				; $B1BB ........
			defb $04,$05,$01,$15,$FF,$08,$00,$15				; $B1C3 ........
			defb $04,$05,$01,$02,$48,$14,$13,$FF				; $B1CB ....H...
			defb $08,$00,$12,$14,$14,$14,$03,$15				; $B1D3 ........
			defb $FF,$0B,$00,$30,$31,$00,$04,$15				; $B1DB ...01...
			defb $FF,$0B,$00,$32,$33,$00,$05,$15				; $B1E3 ...23...
			defb $FF,$0A,$00,$0B,$00,$00,$00,$01				; $B1EB ........
			defb $15,$FF,$06,$00,$A0,$A1,$00,$00				; $B1F3 ........
			defb $0A,$00,$00,$00,$02,$4A,$14,$11				; $B1FB .....J..
			defb $00,$00,$23,$24,$A2,$A3,$00,$00				; $B203 ..#$....
			defb $09,$00,$00,$00,$03,$01,$02,$15				; $B20B ........
			defb $00,$00,$25,$26,$A4,$A5,$00,$06				; $B213 ..%&....
			defb $07,$08,$00,$00,$04,$03,$04,$15				; $B21B ........
			defb $19,$1A,$1B,$1B,$1C,$19,$1A,$FF				; $B223 ........
			defb $05,$1B,$01,$02,$04,$03,$15,$16				; $B22B ........
			defb $17,$18,$16,$17,$18,$16,$17,$15				; $B233 ........
			defb $01,$02,$01,$02,$03,$04,$15,$FF				; $B23B ........
			defb $08,$00,$15,$03,$04,$FF,$04,$14				; $B243 ........
			defb $13,$FF,$08,$00,$12,$14,$14,$00				; $B24B ........
			defb $00,$00,$1D,$FF,$0A,$00,$1F,$FF				; $B253 ........
			defb $1E,$00,$EC,$EC,$EC,$00,$00,$00				; $B25B ........
			defb $23,$24,$FF,$0E,$00,$25,$26,$FF				; $B263 #$...%&.
			defb $0E,$00,$4C,$4D,$00,$00,$00,$E9				; $B26B ..LM....
			defb $00,$EA,$FF,$05,$00,$FF,$05,$1B				; $B273 ........
			defb $1C,$19,$16,$17,$18,$16,$17,$18				; $B27B ........
			defb $16,$17,$18,$01,$02,$03,$15,$01				; $B283 ........
			defb $02,$03,$04,$01,$02,$03,$04,$02				; $B28B ........
			defb $03,$04,$05,$04,$05,$04,$15,$34				; $B293 .......4
			defb $35,$36,$37,$35,$34,$35,$36,$37				; $B29B 56754567
			defb $4E,$4F,$36,$14,$14,$14,$13,$FF				; $B2A3 NO6.....
			defb $09,$00,$50,$51,$FF,$0E,$00,$52				; $B2AB ..PQ...R
			defb $53,$FF,$2F,$00,$A0,$A1,$FF,$08				; $B2B3 S./.....
			defb $00,$27,$28,$00,$46,$46,$00,$A2				; $B2BB .'(.FF..
			defb $A3,$FF,$08,$00,$29,$2A,$46,$4C				; $B2C3 ....)*FL
			defb $4D,$46,$A4,$A5,$16,$17,$18,$16				; $B2CB MF......
			defb $17,$18,$19,$1A,$FF,$08,$1B,$01				; $B2D3 ........
			defb $01,$15,$03,$01,$02,$9E,$FF,$07				; $B2DB ........
			defb $00,$15,$01,$34,$00,$15,$03,$04				; $B2E3 ...4....
			defb $05,$9E,$FF,$07,$00,$15,$02,$00				; $B2EB ........
			defb $00,$15,$01,$02,$03,$9F,$FF,$07				; $B2F3 ........
			defb $00,$15,$03,$00,$00,$15,$04,$05				; $B2FB ........
			defb $01,$15,$FF,$07,$00,$15,$04,$00				; $B303 ........
			defb $00,$12,$14,$14,$14,$13,$FF,$07				; $B30B ........
			defb $00,$15,$01,$FF,$0C,$00,$94,$95				; $B313 ........
			defb $15,$02,$00,$00,$96,$00,$91,$00				; $B31B ........
			defb $91,$FF,$05,$00,$3C,$00,$15,$03				; $B323 ....<...
			defb $00,$00,$15,$93,$92,$93,$92,$0C				; $B32B ........
			defb $0D,$0E,$0D,$0E,$3D,$0C,$15,$04				; $B333 ....=...
			defb $00,$00,$15,$01,$01,$01,$02,$03				; $B33B ........
			defb $04,$05,$01,$02,$03,$04,$15,$01				; $B343 ........
			defb $1C,$19,$15,$02,$03,$01,$02,$03				; $B34B ........
			defb $04,$05,$01,$02,$03,$04,$15,$02				; $B353 ........
			defb $01,$15,$01,$02,$03,$04,$3B,$02				; $B35B ......;.
			defb $3B,$04,$3B,$02,$3B,$04,$15,$01				; $B363 ;.;.;...
			defb $02,$15,$35,$4E,$4F,$35,$3B,$35				; $B36B ..5NO5;5
			defb $3B,$35,$3B,$35,$3D,$34,$15,$02				; $B373 ;5;5=4..
			defb $03,$15,$00,$50,$51,$00,$3D,$00				; $B37B ...PQ.=.
			defb $3B,$00,$3D,$00,$47,$00,$15,$03				; $B383 ;.=.G...
			defb $04,$15,$00,$52,$53,$00,$47,$00				; $B38B ...RS.G.
			defb $3D,$00,$47,$00,$00,$00,$15,$04				; $B393 =.G.....
			defb $01,$15,$FF,$06,$00,$47,$FF,$05				; $B39B .....G..
			defb $00,$15,$01,$02,$15,$FF,$0C,$00				; $B3A3 ........
			defb $15,$02,$03,$15,$FF,$0C,$00,$12				; $B3AB ........
			defb $14,$04,$15,$FF,$09,$00,$E9,$E9				; $B3B3 ........
			defb $00,$00,$00,$01,$15,$FF,$05,$00				; $B3BB ........
			defb $46,$00,$46,$00,$16,$17,$18,$16				; $B3C3 F.F.....
			defb $17,$02,$15,$00,$00,$00,$19,$1A				; $B3CB ........
			defb $FF,$07,$1B,$1C,$19,$01,$15,$FF				; $B3D3 ........
			defb $09,$00,$15,$01,$02,$03,$04,$02				; $B3DB ........
			defb $15,$FF,$09,$00,$15,$04,$05,$01				; $B3E3 ........
			defb $02,$03,$15,$FF,$09,$00,$12,$14				; $B3EB ........
			defb $14,$14,$49,$04,$15,$FF,$0A,$00				; $B3F3 ..I.....
			defb $30,$31,$00,$15,$05,$15,$00,$00				; $B3FB 01......
			defb $00,$A0,$A1,$FF,$05,$00,$32,$33				; $B403 ......23
			defb $00,$15,$01,$15,$00,$00,$00,$A2				; $B40B ........
			defb $A3,$FF,$08,$00,$15,$14,$13,$23				; $B413 .......#
			defb $24,$00,$A4,$A5,$FF,$08,$00,$15				; $B41B $.......
			defb $00,$00,$25,$26,$00,$4E,$4F,$FF				; $B423 ..%&.NO.
			defb $08,$00,$15,$19,$1A,$FF,$05,$1B				; $B42B ........
			defb $1C,$19,$16,$17,$18,$16,$17,$18				; $B433 ........
			defb $15,$01,$02,$03,$04,$01,$02,$03				; $B43B ........
			defb $04,$01,$02,$03,$04,$01,$02,$03				; $B443 ........
			defb $15,$01,$15,$01,$02,$03,$04,$15				; $B44B ........
			defb $01,$02,$03,$15,$05,$04,$03,$02				; $B453 ........
			defb $01,$02,$15,$34,$35,$36,$37,$15				; $B45B ...4567.
			defb $04,$01,$02,$15,$34,$35,$36,$37				; $B463 ....4567
			defb $34,$03,$15,$FF,$04,$00,$12,$14				; $B46B 4.......
			defb $14,$14,$13,$FF,$05,$00,$04,$15				; $B473 ........
			defb $FF,$05,$00,$47,$47,$FF,$07,$00				; $B47B ...GG...
			defb $01,$15,$00,$85,$83,$84,$86,$FF				; $B483 ........
			defb $09,$00,$02,$15,$00,$8B,$00,$00				; $B48B ........
			defb $87,$FF,$09,$00,$03,$15,$00,$87				; $B493 ........
			defb $00,$00,$8B,$FF,$09,$00,$04,$15				; $B49B ........
			defb $00,$87,$00,$00,$87,$00,$46,$46				; $B4A3 ......FF
			defb $FF,$06,$00,$01,$15,$00,$88,$89				; $B4AB ........
			defb $8A,$88,$00,$4C,$4D,$16,$17,$18				; $B4B3 ...LM...
			defb $16,$17,$18,$02,$9A,$FF,$0D,$9B				; $B4BB ........
			defb $9C,$01,$02,$03,$04,$01,$02,$03				; $B4C3 ........
			defb $04,$01,$02,$01,$04,$01,$02,$03				; $B4CB ........
			defb $04,$34,$35,$36,$37,$34,$35,$35				; $B4D3 .4567455
			defb $36,$34,$3C,$35,$3C,$36,$34,$35				; $B4DB 64<5<645
			defb $34,$FF,$09,$00,$3B,$00,$3B,$FF				; $B4E3 4...;.;.
			defb $0D,$00,$3B,$F2,$3D,$FF,$04,$00				; $B4EB ..;.=...
			defb $EE,$FF,$08,$00,$3B,$FF,$0F,$00				; $B4F3 ....;...
			defb $3D,$00,$3C,$FF,$0F,$00,$3B,$FF				; $B4FB =.<...;.
			defb $08,$00,$EB,$FF,$04,$00,$3C,$00				; $B503 ......<.
			defb $3B,$FF,$04,$00,$16,$17,$18,$16				; $B50B ;.......
			defb $17,$18,$16,$17,$18,$3D,$00,$3D				; $B513 .....=.=
			defb $16,$17,$16,$17,$FF,$10,$14,$01				; $B51B ........
			defb $04,$01,$02,$04,$03,$02,$01,$04				; $B523 ........
			defb $03,$02,$01,$04,$02,$03,$01,$34				; $B52B .......4
			defb $35,$36,$37,$34,$35,$35,$35,$36				; $B533 56745556
			defb $37,$34,$35,$36,$37,$34,$34,$EE				; $B53B 7456744.
			defb $EE,$EE,$FF,$1C,$00,$96,$FF,$0F				; $B543 ........
			defb $00,$15,$00,$00,$00,$A0,$A1,$FF				; $B54B ........
			defb $0A,$00,$15,$00,$00,$00,$A2,$A3				; $B553 ........
			defb $00,$00,$46,$46,$00,$46,$46,$00				; $B55B ..FF.FF.
			defb $00,$00,$15,$00,$00,$00,$A4,$A5				; $B563 ........
			defb $00,$00,$4E,$4F,$46,$4E,$4F,$00				; $B56B ..NOFNO.
			defb $00,$00,$15,$19,$1A,$FF,$0A,$1B				; $B573 ........
			defb $1C,$19,$1A,$4B,$01,$02,$03,$04				; $B57B ...K....
			defb $05,$01,$02,$03,$04,$05,$01,$02				; $B583 ........
			defb $03,$04,$05,$04,$01,$02,$03,$04				; $B58B ........
			defb $01,$02,$03,$04,$01,$04,$01,$01				; $B593 ........
			defb $02,$01,$02,$01,$FF,$07,$14,$49				; $B59B .......I
			defb $02,$03,$02,$04,$03,$04,$03,$04				; $B5A3 ........
			defb $FF,$07,$00,$15,$03,$02,$05,$03				; $B5AB ........
			defb $04,$03,$04,$03,$7B,$FF,$06,$00				; $B5B3 ....{...
			defb $15,$FF,$04,$01,$02,$05,$03,$05				; $B5BB ........
			defb $7C,$FF,$06,$00,$15,$01,$04,$02				; $B5C3 |.......
			defb $02,$05,$01,$02,$01,$7D,$FF,$06				; $B5CB .....}..
			defb $00,$15,$02,$02,$02,$05,$02,$02				; $B5D3 ........
			defb $03,$02,$7F,$FF,$06,$00,$12,$FF				; $B5DB ........
			defb $08,$14,$98,$FF,$0F,$00,$96,$FF				; $B5E3 ........
			defb $06,$00,$91,$E9,$91,$E9,$91,$E9				; $B5EB ........
			defb $91,$E9,$10,$15,$19,$1A,$1B,$1B				; $B5F3 ........
			defb $1C,$19,$92,$93,$92,$93,$92,$93				; $B5FB ........
			defb $92,$93,$15,$01,$15,$00,$00,$00				; $B603 ........
			defb $7B,$01,$03,$02,$01,$02,$03,$04				; $B60B {.......
			defb $01,$01,$03,$02,$15,$00,$00,$00				; $B613 ........
			defb $7C,$01,$02,$48,$FF,$05,$14,$49				; $B61B |..H...I
			defb $04,$03,$15,$00,$00,$00,$7D,$03				; $B623 ......}.
			defb $04,$15,$2E,$00,$2E,$00,$2E,$15				; $B62B ........
			defb $01,$01,$15,$00,$00,$00,$7D,$02				; $B633 ......}.
			defb $02,$15,$2F,$E9,$2F,$E9,$2F,$15				; $B63B ../././.
			defb $02,$05,$15,$00,$00,$00,$12,$14				; $B643 ........
			defb $14,$4B,$2E,$2F,$2E,$2F,$2E,$15				; $B64B .K././..
			defb $03,$05,$15,$A0,$A1,$00,$00,$2E				; $B653 ........
			defb $2F,$2E,$2F,$2E,$00,$2E,$00,$15				; $B65B /./.....
			defb $04,$14,$13,$A2,$A3,$00,$00,$2F				; $B663 ......./
			defb $2E,$2F,$2E,$2F,$EA,$2F,$EB,$15				; $B66B ./././..
			defb $05,$00,$00,$A4,$A5,$00,$00,$2E				; $B673 ........
			defb $2F,$2E,$2F,$2E,$2F,$2E,$2F,$15				; $B67B /./././.
			defb $01,$FF,$0E,$14,$4B,$02,$01,$02				; $B683 ....K...
			defb $03,$04,$05,$01,$02,$03,$04,$05				; $B68B ........
			defb $01,$02,$03,$04,$03,$04,$FF,$A0				; $B693 ........
			defb $00,$01,$02,$03,$3B,$05,$3B,$02				; $B69B ....;.;.
			defb $03,$04,$05,$01,$A9,$03,$04,$05				; $B6A3 ........
			defb $01,$01,$48,$A8,$3B,$A8,$3B,$FF				; $B6AB ..H.;.;.
			defb $05,$A8,$13,$34,$35,$36,$37,$02				; $B6B3 ...4567.
			defb $A9,$47,$3D,$00,$3B,$00,$00,$4E				; $B6BB .G=.;..N
			defb $4F,$FF,$06,$00,$03,$A9,$00,$47				; $B6C3 O......G
			defb $00,$3D,$00,$00,$50,$51,$FF,$06				; $B6CB .=..PQ..
			defb $00,$04,$A9,$00,$00,$00,$47,$00				; $B6D3 ......G.
			defb $00,$52,$53,$FF,$06,$00,$05,$A9				; $B6DB .RS.....
			defb $FF,$0E,$00,$01,$A9,$FF,$06,$00				; $B6E3 ........
			defb $27,$28,$FF,$06,$00,$02,$A9,$FF				; $B6EB '(......
			defb $06,$00,$29,$2A,$FF,$06,$00,$03				; $B6F3 ..)*....
			defb $A9,$FF,$05,$00,$10,$14,$14,$14				; $B6FB ........
			defb $11,$0C,$0D,$0E,$0F,$04,$A9,$FF				; $B703 ........
			defb $05,$00,$15,$01,$02,$03,$15,$05				; $B70B ........
			defb $01,$02,$03,$01,$02,$03,$04,$05				; $B713 ........
			defb $01,$02,$03,$04,$05,$01,$02,$03				; $B71B ........
			defb $04,$15,$01,$34,$35,$A6,$A7,$34				; $B723 ...45..4
			defb $A6,$A7,$34,$35,$36,$34,$35,$37				; $B72B ..456457
			defb $34,$15,$01,$00,$00,$47,$47,$00				; $B733 4....GG.
			defb $47,$47,$FF,$07,$00,$15,$02,$FF				; $B73B GG......
			defb $0E,$00,$15,$03,$FF,$0E,$00,$15				; $B743 ........
			defb $04,$FF,$0B,$00,$10,$14,$14,$4B				; $B74B .......K
			defb $01,$FF,$0B,$00,$15,$01,$02,$03				; $B753 ........
			defb $01,$00,$00,$46,$46,$EB,$46,$46				; $B75B ...FF.FF
			defb $FF,$04,$00,$15,$04,$05,$01,$02				; $B763 ........
			defb $0C,$0D,$4E,$4F,$0D,$4E,$4F,$0E				; $B76B ..NO.NO.
			defb $0C,$0D,$0E,$15,$02,$03,$04,$03				; $B773 ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $B77B ........
			defb $05,$01,$02,$15,$04,$05,$02,$04				; $B783 ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $B78B ........
			defb $05,$01,$02,$05,$04,$01,$02,$03				; $B793 ........
			defb $04,$48,$FF,$0E,$A8,$03,$A9,$00				; $B79B .H......
			defb $1D,$1F,$00,$00,$30,$31,$FF,$07				; $B7A3 ....01..
			defb $00,$02,$A9,$FF,$05,$00,$32,$33				; $B7AB ......23
			defb $00,$00,$00,$FF,$04,$EC,$01,$A9				; $B7B3 ........
			defb $FF,$0E,$00,$05,$A9,$FF,$0E,$00				; $B7BB ........
			defb $04,$A9,$FF,$0E,$00,$03,$A9,$FF				; $B7C3 ........
			defb $0D,$00,$7B,$02,$A9,$FF,$0D,$00				; $B7CB ..{.....
			defb $7C,$01,$A9,$FF,$0D,$00,$7D,$01				; $B7D3 |.....}.
			defb $02,$A9,$19,$1A,$FF,$0B,$1B,$A8				; $B7DB ........
			defb $A8,$13,$FF,$29,$00,$FF,$04,$EC				; $B7E3 ...)....
			defb $FF,$21,$00,$EA,$FF,$0E,$00,$14				; $B7EB .!......
			defb $14,$11,$FF,$0D,$00,$02,$03,$15				; $B7F3 ........
			defb $00,$00,$EB,$FF,$0A,$00,$01,$02				; $B7FB ........
			defb $15,$19,$1A,$1B,$1C,$19,$18,$16				; $B803 ........
			defb $17,$18,$16,$17,$18,$16,$FF,$05				; $B80B ........
			defb $1B,$1C,$19,$1A,$FF,$08,$1B,$00				; $B813 ........
			defb $00,$00,$30,$31,$00,$00,$00,$A6				; $B81B ..01....
			defb $A7,$FF,$09,$00,$32,$33,$00,$00				; $B823 ....23..
			defb $00,$47,$47,$FF,$22,$00,$FF,$04				; $B82B .GG."...
			defb $EC,$FF,$38,$00,$0C,$0D,$0C,$0D				; $B833 ..8.....
			defb $0E,$0C,$0E,$0E,$17,$18,$16,$17				; $B83B ........
			defb $18,$16,$17,$0C,$02,$03,$04,$05				; $B843 ........
			defb $02,$03,$01,$01,$FF,$05,$1B,$1C				; $B84B ........
			defb $19,$15,$01,$02,$03,$04,$05,$01				; $B853 ........
			defb $02,$03,$FF,$07,$00,$12,$FF,$06				; $B85B ........
			defb $14,$49,$04,$FF,$0C,$00,$F8,$2F				; $B863 .I...../
			defb $15,$05,$FF,$0C,$00,$2F,$2E,$15				; $B86B ...../..
			defb $01,$FF,$0A,$00,$F8,$2F,$2E,$2F				; $B873 ....././
			defb $15,$02,$FF,$0A,$00,$2F,$2E,$2F				; $B87B ....././
			defb $2E,$15,$04,$00,$00,$00,$46,$46				; $B883 ......FF
			defb $00,$00,$00,$F8,$2F,$2E,$2F,$2E				; $B88B ...././.
			defb $2F,$15,$03,$00,$00,$00,$4C,$4D				; $B893 /.....LM
			defb $00,$F8,$00,$2F,$2E,$2F,$2E,$2F				; $B89B .../././
			defb $2E,$15,$01,$0E,$0C,$10,$FF,$09				; $B8A3 ........
			defb $A8,$49,$2F,$15,$02,$01,$02,$A9				; $B8AB .I/.....
			defb $04,$05,$02,$03,$04,$05,$02,$04				; $B8B3 ........
			defb $05,$A9,$2E,$15,$02,$05,$01,$02				; $B8BB ........
			defb $03,$04,$05,$01,$02,$03,$04,$05				; $B8C3 ........
			defb $01,$02,$03,$02,$01,$04,$48,$FF				; $B8CB ......H.
			defb $0E,$A8,$03,$15,$00,$A6,$A7,$00				; $B8D3 ........
			defb $A6,$A7,$00,$A6,$A7,$00,$A6,$A7				; $B8DB ........
			defb $00,$00,$02,$15,$00,$47,$47,$00				; $B8E3 .....GG.
			defb $47,$47,$00,$47,$47,$00,$47,$47				; $B8EB GG.GG.GG
			defb $00,$7B,$01,$15,$FF,$0D,$00,$7D				; $B8F3 .{.....}
			defb $05,$15,$E9,$EA,$EB,$FF,$0A,$00				; $B8FB ........
			defb $7E,$04,$4A,$14,$14,$14,$11,$FF				; $B903 ~.J.....
			defb $09,$00,$7E,$03,$01,$02,$03,$04				; $B90B ..~.....
			defb $15,$FF,$09,$00,$7D,$02,$48,$14				; $B913 ....}.H.
			defb $14,$14,$13,$FF,$09,$00,$7C,$01				; $B91B ......|.
			defb $15,$FF,$0D,$00,$7D,$01,$02,$03				; $B923 ....}...
			defb $04,$05,$01,$02,$03,$04,$01,$03				; $B92B ........
			defb $03,$04,$05,$7C,$01,$19,$1A,$1B				; $B933 ...|....
			defb $1B,$1B,$1C,$19,$1A,$FF,$04,$1B				; $B93B ........
			defb $1C,$19,$7C,$01,$FF,$08,$00,$47				; $B943 ..|....G
			defb $00,$30,$31,$00,$00,$7D,$02,$14				; $B94B .01..}..
			defb $14,$11,$FF,$07,$00,$32,$33,$00				; $B953 .....23.
			defb $00,$7E,$03,$01,$02,$15,$FF,$0B				; $B95B .~......
			defb $00,$7C,$04,$03,$05,$15,$FF,$0B				; $B963 .|......
			defb $00,$7D,$05,$01,$04,$15,$FF,$0B				; $B96B .}......
			defb $00,$7E,$01,$02,$03,$15,$FF,$0B				; $B973 .~......
			defb $00,$7C,$02,$04,$05,$15,$FF,$0B				; $B97B .|......
			defb $00,$7D,$03,$01,$02,$15,$FF,$04				; $B983 .}......
			defb $ED,$FF,$07,$00,$7E,$04,$01,$A9				; $B98B ....~...
			defb $EF,$EF,$EF,$00,$00,$15,$02,$03				; $B993 ........
			defb $04,$15,$01,$02,$03,$04,$01,$A9				; $B99B ........
			defb $FF,$05,$00,$15,$01,$02,$03,$15				; $B9A3 ........
			defb $34,$35,$36,$34,$02,$A9,$FF,$05				; $B9AB 4564....
			defb $00,$12,$14,$14,$14,$13,$FF,$04				; $B9B3 ........
			defb $00,$03,$A9,$FF,$06,$00,$30,$31				; $B9BB ......01
			defb $FF,$06,$00,$04,$A9,$FF,$06,$00				; $B9C3 ........
			defb $32,$33,$FF,$06,$00,$05,$A9,$FF				; $B9CB 23......
			defb $0E,$00,$01,$A9,$00,$23,$24,$FF				; $B9D3 .....#$.
			defb $0B,$00,$02,$A9,$22,$25,$26,$20				; $B9DB ...."%& 
			defb $FF,$0A,$00,$03,$4A,$FF,$04,$A8				; $B9E3 ....J...
			defb $11,$FF,$09,$00,$04,$01,$02,$03				; $B9EB ........
			defb $04,$01,$A9,$16,$17,$18,$16,$17				; $B9F3 ........
			defb $18,$16,$17,$18,$01,$01,$02,$03				; $B9FB ........
			defb $3B,$05,$3B,$03,$3B,$05,$01,$15				; $BA03 ;.;.;...
			defb $03,$04,$05,$01,$34,$36,$34,$35				; $BA0B ....4645
			defb $3D,$37,$3B,$35,$3B,$37,$34,$15				; $BA13 =7;5;74.
			defb $01,$02,$03,$04,$FF,$04,$00,$47				; $BA1B .......G
			defb $E9,$3D,$EA,$3B,$EB,$00,$12,$A8				; $BA23 .=.;....
			defb $A8,$49,$01,$FF,$06,$00,$47,$00				; $BA2B .I....G.
			defb $3D,$FF,$05,$00,$A9,$02,$FF,$08				; $BA33 =.......
			defb $00,$47,$FF,$05,$00,$A9,$03,$FF				; $BA3B .G......
			defb $0E,$00,$A9,$04,$FF,$0E,$00,$A9				; $BA43 ........
			defb $05,$FF,$0E,$00,$A9,$01,$FF,$0E				; $BA4B ........
			defb $00,$A9,$02,$16,$17,$18,$19,$FF				; $BA53 ........
			defb $04,$ED,$FF,$06,$00,$A9,$02,$01				; $BA5B ........
			defb $A9,$EF,$EF,$EF,$FF,$0A,$00,$9D				; $BA63 ........
			defb $02,$A9,$FF,$0D,$00,$9E,$03,$A9				; $BA6B ........
			defb $FF,$0D,$00,$9E,$04,$A9,$FF,$0D				; $BA73 ........
			defb $00,$9E,$05,$A9,$FF,$05,$00,$27				; $BA7B .......'
			defb $28,$FF,$06,$00,$9E,$01,$A9,$FF				; $BA83 (.......
			defb $05,$00,$29,$2A,$FF,$06,$00,$9E				; $BA8B ..)*....
			defb $02,$4A,$A8,$A8,$9A,$9B,$9C,$A8				; $BA93 .J......
			defb $A8,$11,$FF,$05,$00,$9F,$03,$01				; $BA9B ........
			defb $02,$03,$04,$05,$01,$02,$03,$A9				; $BAA3 ........
			defb $00,$00,$00,$10,$14,$14,$04,$48				; $BAAB .......H
			defb $A8,$A8,$9A,$9B,$9C,$A8,$A8,$13				; $BAB3 ........
			defb $00,$00,$00,$15,$01,$02,$05,$A9				; $BABB ........
			defb $FF,$0B,$00,$15,$03,$04,$01,$9D				; $BAC3 ........
			defb $16,$17,$18,$16,$17,$18,$16,$17				; $BACB ........
			defb $18,$15,$04,$03,$02,$01,$02,$9E				; $BAD3 ........
			defb $FF,$09,$00,$12,$FF,$04,$14,$03				; $BADB ........
			defb $9E,$FF,$0E,$00,$04,$9E,$FF,$0C				; $BAE3 ........
			defb $00,$3C,$00,$05,$9E,$FF,$0C,$00				; $BAEB .<......
			defb $3B,$00,$01,$9E,$FF,$0C,$00,$3B				; $BAF3 ;......;
			defb $00,$02,$9E,$FF,$0C,$00,$3B,$00				; $BAFB ......;.
			defb $03,$9E,$FF,$0C,$00,$3D,$00,$04				; $BB03 .....=..
			defb $9E,$FF,$0B,$00,$10,$14,$14,$05				; $BB0B ........
			defb $9E,$ED,$ED,$ED,$FF,$08,$00,$15				; $BB13 ........
			defb $01,$02,$01,$02,$03,$04,$05,$01				; $BB1B ........
			defb $02,$03,$04,$05,$01,$02,$03,$02				; $BB23 ........
			defb $01,$01,$FF,$05,$14,$9A,$9B,$9B				; $BB2B ........
			defb $9C,$14,$14,$14,$49,$01,$02,$03				; $BB33 ....I...
			defb $00,$00,$00,$FF,$06,$FE,$00,$00				; $BB3B ........
			defb $00,$15,$01,$04,$03,$FF,$0C,$00				; $BB43 ........
			defb $15,$02,$01,$04,$FF,$0C,$00,$12				; $BB4B ........
			defb $14,$14,$14,$97,$98,$FF,$0F,$00				; $BB53 ........
			defb $3C,$FF,$0A,$00,$91,$00,$91,$00				; $BB5B <.......
			defb $00,$3D,$FF,$0A,$00,$92,$93,$92				; $BB63 .=......
			defb $93,$FF,$05,$14,$9A,$9B,$9B,$9C				; $BB6B ........
			defb $FF,$07,$14,$01,$03,$02,$04,$01				; $BB73 ........
			defb $02,$03,$04,$05,$01,$02,$03,$04				; $BB7B ........
			defb $01,$02,$03,$01,$02,$A9,$01,$02				; $BB83 ........
			defb $03,$3B,$34,$A9,$01,$02,$03,$A9				; $BB8B .;4.....
			defb $00,$15,$01,$03,$04,$A9,$34,$35				; $BB93 ......45
			defb $36,$3B,$00,$A9,$04,$05,$01,$A9				; $BB9B 6;......
			defb $00,$15,$02,$05,$01,$A9,$00,$00				; $BBA3 ........
			defb $00,$3B,$F3,$12,$A8,$A8,$A8,$13				; $BBAB .;......
			defb $00,$15,$03,$04,$02,$A9,$00,$00				; $BBB3 ........
			defb $00,$3B,$FF,$07,$00,$15,$04,$14				; $BBBB .;......
			defb $14,$13,$00,$00,$00,$3D,$00,$3C				; $BBC3 .....=.<
			defb $FF,$05,$00,$15,$05,$FF,$04,$00				; $BBCB ........
			defb $A0,$A1,$00,$00,$3B,$FF,$05,$00				; $BBD3 ....;...
			defb $15,$01,$91,$00,$00,$00,$A2,$A3				; $BBDB ........
			defb $3C,$00,$3B,$FF,$05,$00,$15,$02				; $BBE3 <.;.....
			defb $92,$93,$00,$00,$A4,$A5,$3D,$00				; $BBEB ......=.
			defb $3B,$FF,$05,$00,$15,$03,$FF,$04				; $BBF3 ;.......
			defb $14,$9A,$9B,$9C,$11,$3D,$80,$81				; $BBFB .....=..
			defb $82,$10,$14,$4B,$01,$01,$02,$03				; $BC03 ...K....
			defb $04,$05,$01,$02,$15,$04,$05,$01				; $BC0B ........
			defb $02,$15,$04,$05,$01,$05,$A9,$EF				; $BC13 ........
			defb $EF,$EF,$FF,$0A,$00,$7E,$04,$A9				; $BC1B .....~..
			defb $FF,$0D,$00,$7C,$03,$A9,$FF,$0D				; $BC23 ...|....
			defb $00,$7D,$02,$A9,$FF,$0D,$00,$7E				; $BC2B .}.....~
			defb $01,$A9,$FF,$0D,$00,$7C,$04,$A9				; $BC33 .....|..
			defb $FF,$0D,$00,$7D,$03,$A9,$00,$46				; $BC3B ...}...F
			defb $46,$00,$00,$00,$46,$46,$FF,$05				; $BC43 F...FF..
			defb $00,$7E,$02,$A9,$46,$4C,$4D,$00				; $BC4B .~..FLM.
			defb $46,$00,$4C,$4D,$46,$FF,$04,$00				; $BC53 F.LMF...
			defb $7C,$01,$4A,$FF,$09,$A8,$11,$00				; $BC5B |.J.....
			defb $00,$00,$7E,$01,$02,$03,$04,$05				; $BC63 ..~.....
			defb $01,$02,$03,$04,$05,$01,$A9,$00				; $BC6B ........
			defb $00,$00,$7F,$01,$02,$15,$FF,$0B				; $BC73 ........
			defb $00,$7B,$01,$03,$04,$15,$00,$8D				; $BC7B .{......
			defb $FF,$07,$00,$8D,$00,$7C,$02,$05				; $BC83 .....|..
			defb $01,$9D,$8D,$8F,$8D,$FF,$05,$00				; $BC8B ........
			defb $8D,$8F,$8D,$7D,$03,$02,$03,$9E				; $BC93 ...}....
			defb $00,$8D,$FF,$07,$00,$8D,$00,$7E				; $BC9B .......~
			defb $04,$04,$05,$9E,$FF,$0B,$00,$7C				; $BCA3 .......|
			defb $05,$01,$03,$9E,$FF,$0B,$00,$7D				; $BCAB .......}
			defb $01,$04,$02,$9E,$FF,$0B,$00,$7E				; $BCB3 .......~
			defb $02,$01,$02,$9F,$FF,$0B,$00,$7E				; $BCBB .......~
			defb $03,$03,$04,$15,$FF,$0B,$00,$7C				; $BCC3 .......|
			defb $04,$05,$01,$15,$FF,$04,$ED,$FF				; $BCCB ........
			defb $07,$00,$7D,$05,$01,$01,$01,$02				; $BCD3 ..}.....
			defb $03,$04,$05,$01,$02,$03,$15,$01				; $BCDB ........
			defb $02,$A9,$01,$01,$02,$02,$48,$FF				; $BCE3 ......H.
			defb $07,$14,$13,$34,$36,$A9,$02,$02				; $BCEB ...46...
			defb $03,$03,$15,$FF,$09,$00,$FD,$A9				; $BCF3 ........
			defb $03,$03,$04,$04,$15,$FF,$09,$00				; $BCFB ........
			defb $FD,$A9,$04,$04,$05,$01,$15,$FF				; $BD03 ........
			defb $0A,$00,$12,$A8,$A8,$01,$02,$15				; $BD0B ........
			defb $FF,$0D,$00,$02,$03,$15,$FF,$0D				; $BD13 ........
			defb $00,$03,$04,$15,$FF,$08,$00,$46				; $BD1B .......F
			defb $46,$00,$00,$00,$04,$05,$15,$FF				; $BD23 F.......
			defb $07,$00,$10,$FF,$05,$A8,$01,$01				; $BD2B ........
			defb $15,$FF,$07,$00,$A9,$01,$04,$03				; $BD33 ........
			defb $02,$01,$01,$02,$01,$A9,$FF,$0A				; $BD3B ........
			defb $00,$A9,$01,$01,$02,$03,$A9,$8D				; $BD43 ........
			defb $E9,$8D,$E9,$8D,$E9,$8D,$E9,$8D				; $BD4B ........
			defb $00,$A9,$01,$02,$04,$03,$A9,$8F				; $BD53 ........
			defb $8D,$8F,$8D,$8F,$8D,$8F,$8D,$8F				; $BD5B ........
			defb $8D,$9D,$02,$03,$02,$04,$A9,$8D				; $BD63 ........
			defb $00,$8D,$00,$8D,$00,$8D,$00,$8D				; $BD6B ........
			defb $00,$9E,$03,$A8,$A8,$A8,$13,$FF				; $BD73 ........
			defb $0A,$00,$9E,$04,$2E,$EA,$2E,$EB				; $BD7B ........
			defb $2E,$2F,$FF,$04,$00,$94,$95,$00				; $BD83 ./......
			defb $00,$9F,$05,$91,$8D,$91,$8D,$91				; $BD8B ........
			defb $2E,$FF,$04,$00,$96,$00,$00,$00				; $BD93 ........
			defb $A9,$01,$92,$93,$92,$93,$92,$2F				; $BD9B ......./
			defb $FF,$04,$00,$99,$00,$00,$00,$A9				; $BDA3 ........
			defb $02,$FF,$04,$A8,$9A,$FF,$04,$9B				; $BDAB ........
			defb $9C,$FF,$04,$A8,$4B,$03,$01,$02				; $BDB3 ....K...
			defb $03,$04,$05,$01,$02,$03,$04,$05				; $BDBB ........
			defb $01,$02,$03,$04,$05,$01,$01,$A9				; $BDC3 ........
			defb $EF,$EF,$EF,$FF,$08,$00,$15,$01				; $BDCB ........
			defb $01,$02,$A9,$FF,$0B,$00,$15,$01				; $BDD3 ........
			defb $02,$03,$A9,$FF,$0B,$00,$15,$03				; $BDDB ........
			defb $04,$04,$A9,$FF,$0B,$00,$9D,$05				; $BDE3 ........
			defb $01,$05,$A9,$FF,$0B,$00,$9E,$02				; $BDEB ........
			defb $03,$01,$A9,$A0,$A1,$FF,$09,$00				; $BDF3 ........
			defb $9E,$04,$05,$02,$A9,$A2,$A3,$00				; $BDFB ........
			defb $23,$24,$FF,$06,$00,$9F,$01,$02				; $BE03 #$......
			defb $03,$A9,$A4,$A5,$00,$25,$26,$FF				; $BE0B .....%&.
			defb $06,$00,$15,$03,$04,$04,$4A,$FF				; $BE13 ......J.
			defb $08,$A8,$11,$00,$00,$15,$05,$01				; $BE1B ........
			defb $05,$05,$01,$02,$03,$04,$01,$02				; $BE23 ........
			defb $03,$04,$A9,$00,$00,$15,$02,$03				; $BE2B ........
			defb $01,$A9,$FF,$0B,$00,$15,$01,$02				; $BE33 ........
			defb $02,$A9,$2E,$2F,$2E,$2F,$2E,$2F				; $BE3B .../././
			defb $2E,$2F,$2E,$2F,$2E,$15,$03,$05				; $BE43 ././....
			defb $03,$A9,$8D,$8F,$8D,$8F,$8D,$8F				; $BE4B ........
			defb $8D,$8F,$8D,$8F,$8D,$12,$14,$14				; $BE53 ........
			defb $04,$A9,$00,$8D,$00,$8D,$00,$8D				; $BE5B ........
			defb $00,$8D,$00,$8D,$00,$00,$30,$31				; $BE63 ......01
			defb $05,$A9,$FF,$0C,$00,$32,$33,$01				; $BE6B .....23.
			defb $A9,$FF,$06,$00,$46,$46,$FF,$06				; $BE73 ....FF..
			defb $00,$03,$A9,$FF,$05,$00,$46,$4C				; $BE7B ......FL
			defb $4D,$46,$FF,$05,$00,$03,$A9,$00				; $BE83 MF......
			defb $00,$00,$46,$9A,$FF,$04,$9B,$9C				; $BE8B ..F.....
			defb $46,$00,$00,$00,$01,$4A,$A8,$A8				; $BE93 F....J..
			defb $A8,$9A,$FF,$06,$9B,$9C,$A8,$A8				; $BE9B ........
			defb $A8,$01,$02,$03,$04,$05,$01,$02				; $BEA3 ........
			defb $03,$04,$05,$01,$03,$04,$05,$01				; $BEAB ........
			defb $02,$01,$02,$03,$04,$15,$01,$02				; $BEB3 ........
			defb $03,$04,$05,$01,$02,$03,$04,$03				; $BEBB ........
			defb $04,$05,$01,$03,$02,$15,$34,$35				; $BEC3 ......45
			defb $36,$37,$34,$36,$34,$35,$37,$36				; $BECB 67464576
			defb $34,$FF,$04,$14,$13,$FF,$18,$00				; $BED3 4.......
			defb $EC,$EC,$EC,$FF,$12,$00,$A0,$A1				; $BEDB ........
			defb $FF,$0E,$00,$A2,$A3,$00,$46,$46				; $BEE3 ......FF
			defb $00,$46,$46,$00,$00,$27,$28,$FF				; $BEEB .FF..'(.
			defb $04,$00,$A4,$A5,$46,$4C,$4D,$46				; $BEF3 ....FLMF
			defb $4C,$4D,$46,$00,$29,$2A,$00,$00				; $BEFB LMF.)*..
			defb $FF,$10,$A8,$01,$02,$03,$04,$05				; $BF03 ........
			defb $01,$01,$02,$03,$04,$05,$01,$02				; $BF0B ........
			defb $03,$04,$01,$01,$02,$03,$04,$01				; $BF13 ........
			defb $02,$02,$01,$02,$34,$15,$01,$02				; $BF1B ....4...
			defb $03,$04,$01,$34,$35,$36,$37,$35				; $BF23 ...45675
			defb $34,$35,$36,$35,$00,$12,$14,$9A				; $BF2B 4565....
			defb $9B,$9C,$14,$FF,$1C,$00,$FF,$04				; $BF33 ........
			defb $EC,$FF,$40,$00,$FF,$04,$A8,$11				; $BF3B ..@.....
			defb $0D,$0E,$0C,$0D,$0C,$10,$14,$9A				; $BF43 ........
			defb $9B,$9C,$14,$01,$02,$03,$04,$A9				; $BF4B ........
			defb $01,$02,$03,$04,$05,$15,$02,$03				; $BF53 ........
			defb $04,$05,$02,$01,$01,$02,$03,$04				; $BF5B ........
			defb $04,$04,$01,$02,$03,$04,$15,$00				; $BF63 ........
			defb $00,$00,$7C,$14,$9A,$FF,$07,$9B				; $BF6B ..|.....
			defb $9C,$14,$13,$00,$00,$00,$7D,$FF				; $BF73 ......}.
			defb $06,$00,$30,$31,$00,$30,$31,$FF				; $BF7B ..01.01.
			defb $04,$00,$7E,$FF,$06,$00,$32,$33				; $BF83 ..~...23
			defb $00,$32,$33,$FF,$04,$00,$7E,$FF				; $BF8B .23...~.
			defb $0F,$00,$7D,$FF,$0F,$00,$7C,$00				; $BF93 ..}...|.
			defb $00,$00,$27,$28,$FF,$0A,$00,$7E				; $BF9B ..'(...~
			defb $00,$00,$00,$29,$2A,$FF,$0A,$00				; $BFA3 ...)*...
			defb $7D,$14,$9A,$9B,$9C,$14,$11,$0C				; $BFAB }.......
			defb $0D,$0E,$0F,$0C,$0E,$0F,$0C,$0E				; $BFB3 ........
			defb $7C,$02,$01,$02,$03,$04,$15,$01				; $BFBB |.......
			defb $02,$03,$04,$05,$01,$02,$03,$04				; $BFC3 ........
			defb $7F,$04,$05,$15,$FF,$0B,$00,$9D				; $BFCB ........
			defb $01,$01,$02,$15,$00,$00,$A0,$A1				; $BFD3 ........
			defb $FF,$07,$00,$9E,$02,$03,$04,$15				; $BFDB ........
			defb $23,$24,$A2,$A3,$FF,$07,$00,$9E				; $BFE3 #$......
			defb $03,$01,$02,$15,$25,$26,$A4,$A5				; $BFEB ....%&..
			defb $FF,$07,$00,$9E,$04,$03,$04,$4A				; $BFF3 .......J
			defb $FF,$04,$14,$11,$FF,$06,$00,$9E				; $BFFB ........
			defb $01,$01,$02,$01,$02,$03,$04,$05				; $C003 ........
			defb $15,$FF,$06,$00,$9E,$02,$05,$04				; $C00B ........
			defb $48,$FF,$04,$14,$13,$FF,$06,$00				; $C013 H.......
			defb $9E,$03,$03,$02,$15,$1D,$00,$00				; $C01B ........
			defb $1F,$FF,$07,$00,$9E,$04,$01,$04				; $C023 ........
			defb $15,$FF,$0B,$00,$9E,$01,$03,$04				; $C02B ........
			defb $15,$FF,$08,$00,$ED,$ED,$ED,$9F				; $C033 ........
			defb $02,$01,$02,$15,$FF,$07,$00,$A9				; $C03B ........
			defb $01,$02,$03,$04,$05,$03,$04,$15				; $C043 ........
			defb $FF,$07,$00,$12,$FF,$05,$A8,$05				; $C04B ........
			defb $04,$15,$FF,$08,$00,$30,$31,$00				; $C053 .....01.
			defb $00,$00,$14,$14,$13,$FF,$08,$00				; $C05B ........
			defb $32,$33,$00,$00,$00,$7B,$1D,$FF				; $C063 23...{..
			defb $0B,$00,$EC,$EC,$EC,$7C,$00,$00				; $C06B .....|..
			defb $00,$A0,$A1,$FF,$0A,$00,$7D,$00				; $C073 ......}.
			defb $00,$00,$A2,$A3,$FF,$0A,$00,$7D				; $C07B .......}
			defb $00,$00,$00,$A4,$A5,$FF,$0A,$00				; $C083 ........
			defb $9A,$9B,$9C,$14,$14,$14,$9A,$9B				; $C08B ........
			defb $9C,$14,$14,$14,$9A,$9B,$9C,$14				; $C093 ........
			defb $01,$02,$03,$04,$05,$01,$02,$03				; $C09B ........
			defb $04,$05,$01,$02,$03,$04,$05,$01				; $C0A3 ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $C0AB ........
			defb $01,$02,$03,$04,$01,$02,$03,$04				; $C0B3 ........
			defb $FF,$06,$A8,$9A,$9B,$9B,$9B,$9C				; $C0BB ........
			defb $FF,$05,$A8,$FF,$06,$00,$2B,$2C				; $C0C3 ......+,
			defb $00,$2B,$2C,$FF,$0C,$00,$2D,$00				; $C0CB .+,...-.
			defb $00,$2D,$FF,$11,$00,$FF,$04,$EC				; $C0D3 .-......
			defb $FF,$16,$00,$46,$46,$FF,$0D,$00				; $C0DB ...FF...
			defb $46,$4C,$4D,$46,$FF,$07,$00,$FF				; $C0E3 FLMF....
			defb $04,$14,$9A,$FF,$04,$9B,$9C,$FF				; $C0EB ........
			defb $06,$14,$01,$02,$03,$04,$05,$01				; $C0F3 ........
			defb $02,$03,$04,$05,$01,$02,$03,$04				; $C0FB ........
			defb $05,$01,$01,$3B,$03,$3B,$05,$3B				; $C103 ...;.;.;
			defb $02,$3B,$04,$3B,$A9,$00,$00,$15				; $C10B .;.;....
			defb $01,$01,$A8,$3B,$A8,$3B,$A8,$3B				; $C113 ...;.;.;
			defb $A8,$3B,$A8,$3B,$13,$00,$00,$15				; $C11B .;.;....
			defb $01,$01,$00,$3B,$00,$3B,$00,$3B				; $C123 ...;.;.;
			defb $00,$3B,$00,$3B,$00,$00,$00,$15				; $C12B .;.;....
			defb $02,$01,$00,$3D,$00,$3B,$F1,$3D				; $C133 ...=.;.=
			defb $00,$3B,$F6,$3D,$00,$00,$00,$15				; $C13B .;.=....
			defb $03,$02,$00,$EA,$F1,$3B,$00,$8D				; $C143 .....;..
			defb $F2,$3B,$00,$EB,$00,$00,$00,$15				; $C14B .;......
			defb $04,$01,$00,$3C,$00,$3D,$00,$3C				; $C153 ...<.=.<
			defb $00,$3D,$00,$3C,$00,$00,$00,$15				; $C15B .=.<....
			defb $05,$03,$00,$3B,$00,$8D,$00,$3B				; $C163 ...;...;
			defb $00,$8D,$00,$3B,$00,$00,$00,$15				; $C16B ...;....
			defb $01,$04,$00,$3B,$00,$3C,$00,$3B				; $C173 ...;.<.;
			defb $00,$3C,$00,$3B,$00,$00,$00,$15				; $C17B .<.;....
			defb $02,$01,$14,$49,$00,$3B,$00,$3B				; $C183 ...I.;.;
			defb $00,$3B,$00,$3B,$00,$00,$00,$15				; $C18B .;.;....
			defb $03,$05,$01,$15,$0D,$3B,$0D,$3B				; $C193 .....;.;
			defb $0F,$3B,$0C,$3B,$0E,$0C,$0E,$15				; $C19B .;.;....
			defb $04,$01,$7B,$02,$03,$04,$3B,$34				; $C1A3 ..{...;4
			defb $3B,$36,$3B,$34,$3B,$36,$3B,$04				; $C1AB ;6;4;6;.
			defb $05,$01,$7C,$34,$35,$36,$3B,$00				; $C1B3 ..|456;.
			defb $3B,$00,$3B,$00,$3B,$00,$3B,$19				; $C1BB ;.;.;.;.
			defb $1A,$1B,$7D,$00,$00,$00,$3D,$00				; $C1C3 ..}...=.
			defb $3B,$00,$3D,$00,$3B,$00,$3D,$00				; $C1CB ;.=.;.=.
			defb $00,$00,$7E,$00,$00,$00,$8D,$F5				; $C1D3 ..~.....
			defb $3B,$F2,$8D,$F5,$3B,$F2,$8D,$00				; $C1DB ;...;...
			defb $00,$00,$7C,$85,$83,$84,$86,$00				; $C1E3 ..|.....
			defb $3D,$00,$3C,$00,$3D,$00,$3C,$00				; $C1EB =.<.=.<.
			defb $00,$00,$7D,$87,$00,$00,$87,$00				; $C1F3 ..}.....
			defb $8D,$00,$3B,$00,$8D,$00,$3B,$00				; $C1FB ..;...;.
			defb $46,$00,$7E,$8B,$00,$00,$8B,$00				; $C203 F.~.....
			defb $3C,$00,$3B,$00,$3C,$00,$3B,$10				; $C20B <.;.<.;.
			defb $14,$14,$7C,$87,$00,$00,$87,$00				; $C213 ..|.....
			defb $3B,$00,$3B,$00,$3B,$00,$3B,$15				; $C21B ;.;.;.;.
			defb $01,$01,$7D,$88,$89,$8A,$88,$0C				; $C223 ..}.....
			defb $3B,$0F,$3B,$0E,$3B,$0C,$3B,$15				; $C22B ;.;.;.;.
			defb $05,$02,$7D,$02,$03,$04,$05,$01				; $C233 ..}.....
			defb $3B,$03,$3B,$05,$3B,$02,$3B,$15				; $C23B ;.;.;.;.
			defb $04,$03,$01,$02,$03,$04,$04,$05				; $C243 ........
			defb $01,$02,$03,$04,$04,$05,$01,$02				; $C24B ........
			defb $05,$04,$FF,$06,$1B,$1C,$19,$1A				; $C253 ........
			defb $FF,$05,$1B,$1C,$19,$FF,$04,$EE				; $C25B ........
			defb $47,$47,$00,$00,$00,$47,$47,$FF				; $C263 GG...GG.
			defb $2B,$00,$A0,$A1,$FF,$08,$00,$14				; $C26B +.......
			defb $14,$14,$11,$23,$24,$A2,$A3,$FF				; $C273 ...#$...
			defb $08,$00,$01,$02,$03,$15,$25,$26				; $C27B ......%&
			defb $A4,$A5,$00,$46,$EA,$46,$EB,$00				; $C283 ...F.F..
			defb $00,$00,$04,$05,$01,$4A,$14,$14				; $C28B .....J..
			defb $14,$9A,$FF,$04,$9B,$9C,$14,$14				; $C293 ........
			defb $14,$01,$02,$03,$04,$05,$01,$02				; $C29B ........
			defb $03,$04,$05,$01,$05,$02,$01,$05				; $C2A3 ........
			defb $01,$01,$02,$03,$04,$04,$01,$03				; $C2AB ........
			defb $04,$05,$01,$A9,$05,$04,$03,$02				; $C2B3 ........
			defb $01,$FF,$0A,$A8,$13,$34,$35,$36				; $C2BB .....456
			defb $37,$34,$FF,$04,$EE,$FF,$04,$00				; $C2C3 74......
			defb $30,$31,$FF,$0E,$00,$32,$33,$FF				; $C2CB 01...23.
			defb $2E,$00,$27,$28,$FF,$09,$00,$20				; $C2D3 ..'(... 
			defb $00,$22,$00,$00,$29,$2A,$FF,$06				; $C2DB ."..)*..
			defb $00,$FF,$0A,$14,$11,$0C,$0D,$0E				; $C2E3 ........
			defb $0F,$0C,$01,$02,$03,$04,$05,$01				; $C2EB ........
			defb $02,$04,$03,$01,$15,$02,$01,$03				; $C2F3 ........
			defb $02,$01,$01,$02,$03,$04,$05,$01				; $C2FB ........
			defb $02,$03,$04,$05,$15,$01,$01,$02				; $C303 ........
			defb $03,$04,$34,$36,$34,$35,$36,$37				; $C30B ..464567
			defb $35,$36,$37,$37,$15,$03,$04,$03				; $C313 5677....
			defb $02,$01,$FF,$0A,$00,$15,$03,$01				; $C31B ........
			defb $02,$03,$04,$FF,$0A,$00,$15,$04				; $C323 ........
			defb $04,$03,$02,$01,$FF,$0A,$00,$12				; $C32B ........
			defb $FF,$05,$14,$97,$98,$FF,$0F,$00				; $C333 ........
			defb $91,$FF,$08,$00,$91,$00,$91,$00				; $C33B ........
			defb $91,$00,$0C,$92,$0E,$0E,$80,$81				; $C343 ........
			defb $82,$0E,$0D,$0C,$92,$93,$92,$93				; $C34B ........
			defb $92,$93,$01,$02,$03,$04,$02,$02				; $C353 ........
			defb $03,$04,$05,$01,$02,$01,$04,$03				; $C35B ........
			defb $02,$01,$01,$02,$03,$04,$04,$01				; $C363 ........
			defb $02,$03,$04,$05,$01,$03,$04,$03				; $C36B ........
			defb $02,$03,$01,$02,$15,$FF,$0B,$00				; $C373 ........
			defb $15,$01,$03,$04,$15,$FF,$07,$00				; $C37B ........
			defb $8D,$8F,$8D,$00,$15,$02,$01,$02				; $C383 ........
			defb $4A,$FF,$05,$14,$11,$FF,$05,$8D				; $C38B J.......
			defb $9D,$03,$03,$04,$01,$02,$03,$04				; $C393 ........
			defb $05,$01,$15,$8D,$8F,$8D,$8F,$8D				; $C39B ........
			defb $9E,$04,$14,$14,$9A,$9B,$9B,$9C				; $C3A3 ........
			defb $14,$14,$13,$00,$8D,$00,$8D,$00				; $C3AB ........
			defb $9E,$05,$FF,$0C,$00,$94,$95,$9F				; $C3B3 ........
			defb $01,$91,$00,$91,$00,$91,$FF,$07				; $C3BB ........
			defb $00,$93,$00,$15,$02,$92,$93,$92				; $C3C3 ........
			defb $93,$92,$FF,$07,$00,$93,$00,$15				; $C3CB ........
			defb $03,$FF,$05,$A8,$9A,$9B,$9B,$9B				; $C3D3 ........
			defb $9C,$FF,$04,$A8,$4B,$04,$01,$02				; $C3DB ....K...
			defb $03,$04,$05,$01,$02,$03,$04,$05				; $C3E3 ........
			defb $01,$02,$03,$04,$05,$03,$00,$00				; $C3EB ........
			defb $00,$00,$00,$00,$00,$00,$00,$10				; $C3F3 ........
			defb $10,$10,$10,$00,$10,$00,$00,$24				; $C3FB .......$
			defb $24,$00,$00,$00,$00,$00,$00,$10				; $C403 $.......
			defb $00,$00,$00,$00,$00,$00,$00,$10				; $C40B ........
			defb $00,$00,$00,$00,$00,$00,$00,$10				; $C413 ........
			defb $00,$00,$00,$00,$00,$00,$00,$10				; $C41B ........
			defb $00,$00,$00,$00,$00,$00,$08,$08				; $C423 ........
			defb $10,$00,$00,$00,$00,$00,$00,$1C				; $C42B ........
			defb $10,$10,$10,$10,$1C,$00,$00,$38				; $C433 .......8
			defb $08,$08,$08,$08,$38,$00,$FF,$81				; $C43B ....8...
			defb $BD,$A1,$A1,$BD,$81,$FF,$00,$00				; $C443 ........
			defb $08,$08,$3E,$08,$08,$00,$00,$00				; $C44B ..>.....
			defb $00,$00,$00,$08,$08,$10,$00,$00				; $C453 ........
			defb $00,$7E,$00,$00,$00,$00,$00,$00				; $C45B .~......
			defb $00,$00,$00,$00,$18,$00,$00,$02				; $C463 ........
			defb $04,$08,$10,$20,$40,$00,$00,$7E				; $C46B ... @..~
			defb $46,$4A,$52,$62,$7E,$00,$00,$10				; $C473 FJRb~...
			defb $30,$10,$10,$10,$7C,$00,$00,$7E				; $C47B 0...|..~
			defb $02,$7E,$40,$40,$7E,$00,$00,$7E				; $C483 .~@@~..~
			defb $02,$3C,$02,$02,$7E,$00,$00,$42				; $C48B .<..~..B
			defb $42,$7E,$02,$02,$02,$00,$00,$7E				; $C493 B~.....~
			defb $40,$7E,$02,$02,$7E,$00,$00,$7E				; $C49B @~..~..~
			defb $40,$7E,$42,$42,$7E,$00,$00,$7E				; $C4A3 @~BB~..~
			defb $02,$02,$02,$02,$02,$00,$00,$7E				; $C4AB .......~
			defb $42,$7E,$42,$42,$7E,$00,$00,$7E				; $C4B3 B~BB~..~
			defb $42,$42,$7E,$02,$02,$00,$00,$00				; $C4BB BB~.....
			defb $00,$38,$38,$00,$38,$38,$00,$00				; $C4C3 .88.88..
			defb $38,$38,$00,$38,$38,$70,$00,$1C				; $C4CB 88.88p..
			defb $38,$70,$E0,$70,$38,$1C,$00,$00				; $C4D3 8p.p8...
			defb $FE,$00,$00,$FE,$00,$00,$00,$70				; $C4DB .......p
			defb $38,$1C,$0E,$1C,$38,$70,$00,$7C				; $C4E3 8...8p.|
			defb $E6,$0C,$38,$38,$00,$38,$00,$7C				; $C4EB ..88.8.|
			defb $E6,$EE,$EA,$EE,$E0,$7C,$00,$7E				; $C4F3 .....|.~
			defb $42,$7E,$42,$42,$42,$00,$00,$7E				; $C4FB B~BBB..~
			defb $42,$7C,$42,$42,$7E,$00,$00,$7E				; $C503 B|BB~..~
			defb $40,$40,$40,$40,$7E,$00,$00,$7C				; $C50B @@@@~..|
			defb $42,$42,$42,$42,$7E,$00,$00,$7E				; $C513 BBBB~..~
			defb $40,$7E,$40,$40,$7E,$00,$00,$7E				; $C51B @~@@~..~
			defb $40,$7E,$40,$40,$40,$00,$00,$7E				; $C523 @~@@@..~
			defb $42,$40,$46,$42,$7E,$00,$00,$42				; $C52B B@FB~..B
			defb $42,$7E,$42,$42,$42,$00,$00,$7C				; $C533 B~BBB..|
			defb $10,$10,$10,$10,$7C,$00,$00,$7C				; $C53B ....|..|
			defb $10,$10,$10,$10,$70,$00,$00,$44				; $C543 ....p..D
			defb $48,$70,$48,$44,$44,$00,$00,$40				; $C54B HpHDD..@
			defb $40,$40,$40,$40,$7C,$00,$00,$42				; $C553 @@@@|..B
			defb $66,$5A,$42,$42,$42,$00,$00,$42				; $C55B fZBBB..B
			defb $62,$52,$4A,$46,$42,$00,$00,$7E				; $C563 bRJFB..~
			defb $42,$42,$42,$42,$7E,$00,$00,$7E				; $C56B BBBB~..~
			defb $42,$42,$7E,$40,$40,$00,$00,$7E				; $C573 BB~@@..~
			defb $42,$42,$42,$44,$7A,$00,$00,$7E				; $C57B BBBDz..~
			defb $42,$7E,$48,$44,$42,$00,$00,$7E				; $C583 B~HDB..~
			defb $40,$7E,$02,$02,$7E,$00,$00,$7C				; $C58B @~..~..|
			defb $10,$10,$10,$10,$10,$00,$00,$42				; $C593 .......B
			defb $42,$42,$42,$42,$7E,$00,$00,$42				; $C59B BBBB~..B
			defb $42,$42,$42,$24,$18,$00,$00,$42				; $C5A3 BBB$...B
			defb $42,$42,$5A,$66,$42,$00,$00,$42				; $C5AB BBZfB..B
			defb $24,$18,$18,$24,$42,$00,$00,$44				; $C5B3 $..$B..D
			defb $28,$10,$10,$10,$10,$00,$00,$7E				; $C5BB (......~
			defb $04,$08,$10,$20,$7E,$00,$63,$63				; $C5C3 ... ~.cc
			defb $63,$63,$63,$63,$63,$63,$0E,$3E				; $C5CB cccccc.>
			defb $7E,$FE,$00,$FF,$7F,$0F,$C0,$50				; $C5D3 ~......P
			defb $AC,$55,$00,$FF,$FC,$E0,$00,$77				; $C5DB .U.....w
			defb $EF,$5F,$00,$7B,$FB,$7B,$00,$C0				; $C5E3 ._.{.{..
			defb $BA,$00,$00,$00,$6A,$00,$00,$40				; $C5EB ....j..@
			defb $20,$10,$08,$04,$02,$01,$00,$02				; $C5F3  .......
			defb $04,$08,$10,$20,$40,$00,$00,$02				; $C5FB ... @...
			defb $3C,$1C,$0C,$24,$40,$00,$80,$40				; $C603 <..$@..@
			defb $3C,$38,$30,$24,$02,$00,$00,$00				; $C60B <80$....
			defb $00,$00,$7F,$00,$00,$00,$00,$00				; $C613 ........
			defb $60,$78,$7E,$78,$60,$00,$00,$07				; $C61B `x~x`...
			defb $0F,$18,$32,$36,$36,$34,$00,$C0				; $C623 ..2664..
			defb $E0,$30,$18,$18,$18,$18,$34,$30				; $C62B .0....40
			defb $30,$30,$18,$0F,$07,$00,$18,$58				; $C633 00.....X
			defb $58,$98,$30,$E0,$C0,$00,$1F,$05				; $C63B X.0.....
			defb $05,$05,$02,$02,$01,$00,$FF,$84				; $C643 ........
			defb $84,$84,$C8,$C8,$10,$E0,$01,$02				; $C64B ........
			defb $02,$04,$04,$05,$1F,$00,$10,$08				; $C653 ........
			defb $08,$44,$E4,$F4,$FF,$00,$00,$00				; $C65B .D......
			defb $00,$00,$0C,$3C,$FC,$FC,$0C,$3C				; $C663 ...<...<
			defb $FC,$FC,$FC,$FC,$FC,$FC,$00,$00				; $C66B ........
			defb $00,$00,$0C,$3C,$FC,$00,$0C,$3C				; $C673 ...<...<
			defb $FC,$FC,$FC,$FC,$FC,$00,$FC,$FC				; $C67B ........
			defb $FC,$FC,$FC,$FC,$FC,$00,$FC,$FC				; $C683 ........
			defb $FC,$FC,$FC,$FC,$FC,$00,$0E,$3E				; $C68B .......>
			defb $5E,$6E,$F6,$FA,$FC,$00,$00,$FF				; $C693 ^n......
			defb $00,$FF,$AA,$55,$00,$00,$70,$7C				; $C69B ...U..p|
			defb $7A,$76,$6F,$5F,$3F,$00,$00,$FC				; $C6A3 zvo_?...
			defb $FA,$F6,$6E,$5E,$3E,$0E,$00,$00				; $C6AB ..n^>...
			defb $FF,$00,$FF,$55,$AA,$00,$00,$3F				; $C6B3 ...U...?
			defb $5F,$6F,$76,$7A,$7C,$70,$5A,$5C				; $C6BB _ovz|pZ\
			defb $5A,$5C,$5A,$5C,$5A,$5C,$7E,$40				; $C6C3 Z\Z\Z\~@
			defb $4A,$54,$4A,$54,$4A,$54,$00,$34				; $C6CB JTJTJT.4
			defb $38,$34,$38,$34,$38,$34,$38,$34				; $C6D3 84848484
			defb $38,$34,$28,$14,$28,$00,$7E,$40				; $C6DB 84(.(.~@
			defb $4A,$54,$4A,$54,$4A,$54,$00,$BF				; $C6E3 JTJTJT..
			defb $00,$5F,$AA,$55,$00,$00,$00,$00				; $C6EB ._.U....
			defb $BF,$00,$AF,$55,$AA,$00,$00,$00				; $C6F3 ...U....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C6FB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C703 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C70B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C713 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C71B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C723 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C72B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C733 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C73B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C743 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C74B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C753 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C75B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C763 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C76B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C773 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C77B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C783 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C78B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C793 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C79B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C7A3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $C7AB ........
			defb $00,$00,$00,$00,$00,$00,$0E,$C0				; $C7B3 ........
			defb $00,$3E,$50,$00,$7E,$AC,$00,$FE				; $C7BB .>P.~...
			defb $55,$00,$00,$00,$00,$FF,$FF,$00				; $C7C3 U.......
			defb $7F,$FC,$00,$0F,$E0,$00,$00,$00				; $C7CB ........
			defb $00,$77,$C0,$00,$EF,$BA,$00,$5F				; $C7D3 .w....._
			defb $00,$00,$00,$00,$00,$7B,$00,$00				; $C7DB .....{..
			defb $FB,$6A,$00,$7B,$00,$00,$03,$B0				; $C7E3 .j.{....
			defb $00,$0F,$94,$00,$1F,$AB,$00,$3F				; $C7EB .......?
			defb $95,$40,$00,$00,$00,$3F,$FF,$C0				; $C7F3 .@...?..
			defb $1F,$FF,$00,$03,$F8,$00,$00,$00				; $C7FB ........
			defb $00,$1D,$F0,$00,$3B,$EE,$80,$17				; $C803 ....;...
			defb $C0,$00,$00,$00,$00,$1E,$C0,$00				; $C80B ........
			defb $3E,$DA,$80,$1E,$C0,$00,$00,$EC				; $C813 >.......
			defb $00,$03,$E5,$00,$07,$EA,$C0,$0F				; $C81B ........
			defb $E5,$50,$00,$00,$00,$0F,$FF,$F0				; $C823 .P......
			defb $07,$FF,$C0,$00,$FE,$00,$00,$00				; $C82B ........
			defb $00,$07,$7C,$00,$0E,$FB,$A0,$05				; $C833 ..|.....
			defb $F0,$00,$00,$00,$00,$07,$B0,$00				; $C83B ........
			defb $0F,$B6,$A0,$07,$B0,$00,$00,$3B				; $C843 .......;
			defb $00,$00,$F9,$40,$01,$FA,$B0,$03				; $C84B ...@....
			defb $F9,$54,$00,$00,$00,$03,$FF,$FC				; $C853 .T......
			defb $01,$FF,$F0,$00,$3F,$80,$00,$00				; $C85B ....?...
			defb $00,$01,$DF,$00,$03,$BE,$E8,$01				; $C863 ........
			defb $7C,$00,$00,$00,$00,$01,$EC,$00				; $C86B |.......
			defb $03,$ED,$A8,$01,$EC,$00,$03,$70				; $C873 .......p
			defb $00,$0A,$7C,$00,$35,$7E,$00,$AA				; $C87B ..|.5~..
			defb $7F,$00,$00,$00,$00,$FF,$FF,$00				; $C883 ........
			defb $3F,$FE,$00,$07,$F0,$00,$00,$00				; $C88B ?.......
			defb $00,$03,$EE,$00,$5D,$F7,$00,$00				; $C893 ....]...
			defb $FA,$00,$00,$00,$00,$00,$DE,$00				; $C89B ........
			defb $56,$DF,$00,$00,$DE,$00,$00,$DC				; $C8A3 V.......
			defb $00,$02,$9F,$00,$0D,$5F,$80,$2A				; $C8AB ....._.*
			defb $9F,$C0,$00,$00,$00,$3F,$FF,$C0				; $C8B3 .....?..
			defb $0F,$FF,$80,$01,$FC,$00,$00,$00				; $C8BB ........
			defb $00,$00,$FB,$80,$17,$7D,$C0,$00				; $C8C3 .....}..
			defb $3E,$80,$00,$00,$00,$00,$37,$80				; $C8CB >.....7.
			defb $15,$B7,$C0,$00,$37,$80,$00,$37				; $C8D3 ....7..7
			defb $00,$00,$A7,$C0,$03,$57,$E0,$0A				; $C8DB .....W..
			defb $A7,$F0,$00,$00,$00,$0F,$FF,$F0				; $C8E3 ........
			defb $03,$FF,$E0,$00,$7F,$00,$00,$00				; $C8EB ........
			defb $00,$00,$3E,$E0,$05,$DF,$70,$00				; $C8F3 ..>...p.
			defb $0F,$A0,$00,$00,$00,$00,$0D,$E0				; $C8FB ........
			defb $05,$6D,$F0,$00,$0D,$E0,$00,$0D				; $C903 .m......
			defb $C0,$00,$29,$F0,$00,$D5,$F8,$02				; $C90B ..).....
			defb $A9,$FC,$00,$00,$00,$03,$FF,$FC				; $C913 ........
			defb $00,$FF,$F8,$00,$1F,$C0,$00,$00				; $C91B ........
			defb $00,$00,$0F,$B8,$01,$77,$DC,$00				; $C923 .....w..
			defb $03,$E8,$00,$00,$00,$00,$03,$78				; $C92B .......x
			defb $01,$5B,$7C,$00,$03,$78,$07,$E0				; $C933 .[|..x..
			defb $00,$1F,$E8,$00,$3F,$F4,$00,$7F				; $C93B ....?...
			defb $EA,$00,$7F,$D6,$00,$9F,$E9,$00				; $C943 ........
			defb $E7,$A7,$00,$F8,$1F,$00,$FF,$FF				; $C94B ........
			defb $00,$3C,$3C,$00,$DB,$DB,$00,$DB				; $C953 .<<.....
			defb $DB,$00,$00,$00,$00,$00,$00,$00				; $C95B ........
			defb $00,$00,$00,$00,$00,$00,$01,$F8				; $C963 ........
			defb $00,$07,$FA,$00,$0F,$FD,$00,$1F				; $C96B ........
			defb $FA,$80,$1F,$F5,$80,$27,$FA,$40				; $C973 .....'.@
			defb $39,$E9,$C0,$3E,$07,$C0,$0F,$FF				; $C97B 9..>....
			defb $C0,$37,$0F,$00,$36,$F6,$C0,$06				; $C983 .7..6...
			defb $F6,$C0,$00,$00,$00,$00,$00,$00				; $C98B ........
			defb $00,$00,$00,$00,$00,$00,$00,$7E				; $C993 .......~
			defb $00,$01,$FE,$80,$03,$FF,$40,$07				; $C99B ......@.
			defb $FE,$A0,$07,$FD,$60,$09,$FE,$90				; $C9A3 ....`...
			defb $0E,$7A,$70,$0F,$81,$F0,$0F,$C3				; $C9AB .zp.....
			defb $F0,$03,$BD,$C0,$0D,$BD,$B0,$0D				; $C9B3 ........
			defb $81,$B0,$00,$00,$00,$00,$00,$00				; $C9BB ........
			defb $00,$00,$00,$00,$00,$00,$00,$1F				; $C9C3 ........
			defb $80,$00,$7F,$A0,$00,$FF,$D0,$01				; $C9CB ........
			defb $FF,$A8,$01,$FF,$58,$02,$7F,$A4				; $C9D3 ....X...
			defb $03,$9E,$9C,$03,$E0,$7C,$03,$FF				; $C9DB .....|..
			defb $F0,$00,$F0,$EC,$03,$6F,$6C,$03				; $C9E3 .....ol.
			defb $6F,$60,$00,$00,$00,$00,$00,$00				; $C9EB o`......
			defb $00,$00,$00,$00,$00,$00,$07,$B0				; $C9F3 ........
			defb $00,$1B,$B0,$00,$3B,$C0,$00,$7D				; $C9FB ....;..}
			defb $F0,$00,$7D,$F0,$00,$FE,$C0,$00				; $CA03 ..}.....
			defb $FE,$B0,$00,$FE,$B0,$00,$FE,$B0				; $CA0B ........
			defb $00,$FC,$B0,$00,$F6,$C0,$00,$29				; $CA13 .......)
			defb $F0,$00,$55,$F0,$00,$2B,$C0,$00				; $CA1B ..U..+..
			defb $1B,$B0,$00,$07,$B0,$00,$01,$EC				; $CA23 ........
			defb $00,$06,$EC,$00,$0E,$F0,$00,$1F				; $CA2B ........
			defb $7C,$00,$1F,$7C,$00,$3F,$B0,$00				; $CA33 |..|.?..
			defb $3F,$AC,$00,$3F,$AC,$00,$3F,$AC				; $CA3B ?..?..?.
			defb $00,$3F,$2C,$00,$3D,$B0,$00,$0A				; $CA43 .?,.=...
			defb $7C,$00,$15,$7C,$00,$0A,$F0,$00				; $CA4B |..|....
			defb $06,$EC,$00,$01,$EC,$00,$00,$7B				; $CA53 .......{
			defb $00,$01,$BB,$00,$03,$BC,$00,$07				; $CA5B ........
			defb $DF,$00,$07,$DF,$00,$0F,$EC,$00				; $CA63 ........
			defb $0F,$EB,$00,$0F,$EB,$00,$0F,$EB				; $CA6B ........
			defb $00,$0F,$CB,$00,$0F,$6C,$00,$02				; $CA73 .....l..
			defb $9F,$00,$05,$5F,$00,$02,$BC,$00				; $CA7B ..._....
			defb $01,$BB,$00,$00,$7B,$00,$00,$1E				; $CA83 ....{...
			defb $C0,$00,$6E,$C0,$00,$EF,$00,$01				; $CA8B ..n.....
			defb $F7,$C0,$01,$F7,$C0,$03,$FB,$00				; $CA93 ........
			defb $03,$FA,$C0,$03,$FA,$C0,$03,$FA				; $CA9B ........
			defb $C0,$03,$F2,$C0,$03,$DB,$00,$00				; $CAA3 ........
			defb $A7,$C0,$01,$57,$C0,$00,$AF,$00				; $CAAB ...W....
			defb $00,$6E,$C0,$00,$1E,$C0,$00,$00				; $CAB3 .n......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CABB ........
			defb $00,$00,$DB,$DB,$00,$DB,$DB,$00				; $CAC3 ........
			defb $3C,$3C,$00,$FF,$FF,$00,$F8,$1F				; $CACB <<......
			defb $00,$E7,$A7,$00,$9F,$E9,$00,$7F				; $CAD3 ........
			defb $D6,$00,$7F,$EA,$00,$3F,$F4,$00				; $CADB .....?..
			defb $1F,$E8,$00,$07,$E0,$00,$00,$00				; $CAE3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CAEB ........
			defb $00,$00,$06,$F6,$C0,$36,$F6,$C0				; $CAF3 .....6..
			defb $37,$0F,$00,$0F,$FF,$C0,$3E,$07				; $CAFB 7.....>.
			defb $C0,$39,$E9,$C0,$27,$FA,$40,$1F				; $CB03 .9..'.@.
			defb $F5,$80,$1F,$FA,$80,$0F,$FD,$00				; $CB0B ........
			defb $07,$FA,$00,$01,$F8,$00,$00,$00				; $CB13 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CB1B ........
			defb $00,$00,$0D,$81,$B0,$0D,$BD,$B0				; $CB23 ........
			defb $03,$BD,$C0,$0F,$C3,$F0,$0F,$81				; $CB2B ........
			defb $F0,$0E,$7A,$70,$09,$FE,$90,$07				; $CB33 ..zp....
			defb $FD,$60,$07,$FE,$A0,$03,$FF,$40				; $CB3B .`.....@
			defb $01,$FE,$80,$00,$7E,$00,$00,$00				; $CB43 ....~...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CB4B ........
			defb $00,$00,$03,$6F,$60,$03,$6F,$6C				; $CB53 ...o`.ol
			defb $00,$F0,$EC,$03,$FF,$F0,$03,$E0				; $CB5B ........
			defb $7C,$03,$9E,$9C,$02,$7F,$A4,$01				; $CB63 |.......
			defb $FF,$58,$01,$FF,$A8,$00,$FF,$D0				; $CB6B .X......
			defb $00,$7F,$A0,$00,$1F,$80,$0D,$E0				; $CB73 ........
			defb $00,$0D,$D8,$00,$03,$DC,$00,$0F				; $CB7B ........
			defb $BE,$00,$0F,$BE,$00,$03,$7F,$00				; $CB83 ........
			defb $0D,$7F,$00,$0D,$7F,$00,$0D,$7F				; $CB8B ........
			defb $00,$0D,$3F,$00,$03,$6F,$00,$0F				; $CB93 ..?..o..
			defb $94,$00,$0F,$AA,$00,$03,$D4,$00				; $CB9B ........
			defb $0D,$D8,$00,$0D,$E0,$00,$03,$78				; $CBA3 .......x
			defb $00,$03,$76,$00,$00,$F7,$00,$03				; $CBAB ..v.....
			defb $EF,$80,$03,$EF,$80,$00,$DF,$C0				; $CBB3 ........
			defb $03,$5F,$C0,$03,$5F,$C0,$03,$5F				; $CBBB ._.._.._
			defb $C0,$03,$4F,$C0,$00,$DB,$C0,$03				; $CBC3 ..O.....
			defb $E5,$00,$03,$EA,$80,$00,$F5,$00				; $CBCB ........
			defb $03,$76,$00,$03,$78,$00,$00,$DE				; $CBD3 .v..x...
			defb $00,$00,$DD,$80,$00,$3D,$C0,$00				; $CBDB .....=..
			defb $FB,$E0,$00,$FB,$E0,$00,$37,$F0				; $CBE3 ......7.
			defb $00,$D7,$F0,$00,$D7,$F0,$00,$D7				; $CBEB ........
			defb $F0,$00,$D3,$F0,$00,$36,$F0,$00				; $CBF3 .....6..
			defb $F9,$40,$00,$FA,$A0,$00,$3D,$40				; $CBFB .@....=@
			defb $00,$DD,$80,$00,$DE,$00,$00,$37				; $CC03 .......7
			defb $80,$00,$37,$60,$00,$0F,$70,$00				; $CC0B ..7`..p.
			defb $3E,$F8,$00,$3E,$F8,$00,$0D,$FC				; $CC13 >..>....
			defb $00,$35,$FC,$00,$35,$FC,$00,$35				; $CC1B .5..5..5
			defb $FC,$00,$34,$FC,$00,$0D,$BC,$00				; $CC23 ..4.....
			defb $3E,$50,$00,$3E,$A8,$00,$0F,$50				; $CC2B >P.>...P
			defb $00,$37,$60,$00,$37,$80,$00,$50				; $CC33 .7`.7..P
			defb $00,$00,$22,$00,$03,$05,$00,$07				; $CC3B ..".....
			defb $56,$00,$BB,$76,$00,$07,$56,$00				; $CC43 V..v..V.
			defb $03,$05,$00,$00,$22,$00,$00,$50				; $CC4B ...."..P
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CC53 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CC5B ........
			defb $00,$00,$00,$00,$00,$00,$00,$14				; $CC63 ........
			defb $00,$00,$1C,$80,$00,$C9,$40,$01				; $CC6B ......@.
			defb $C1,$80,$2E,$D5,$80,$01,$DD,$80				; $CC73 ........
			defb $00,$D5,$40,$00,$00,$80,$00,$1C				; $CC7B ..@.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CC83 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CC8B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CC93 ........
			defb $00,$00,$05,$20,$00,$37,$50,$00				; $CC9B ... .7P.
			defb $75,$60,$0B,$B0,$60,$00,$75,$60				; $CCA3 u`..`.u`
			defb $00,$37,$50,$00,$05,$20,$00,$00				; $CCAB .7P.. ..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CCB3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CCBB ........
			defb $00,$00,$00,$00,$00,$00,$00,$01				; $CCC3 ........
			defb $C0,$00,$00,$08,$00,$0D,$54,$00				; $CCCB ......T.
			defb $1D,$D8,$02,$ED,$58,$00,$1C,$18				; $CCD3 ....X...
			defb $00,$0C,$94,$00,$01,$C8,$00,$01				; $CCDB ........
			defb $40,$00,$00,$00,$00,$00,$00,$00				; $CCE3 @.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CCEB ........
			defb $00,$00,$00,$00,$00,$00,$0E,$00				; $CCF3 ........
			defb $00,$40,$00,$00,$AA,$C0,$00,$6E				; $CCFB .@.....n
			defb $E0,$00,$6A,$DD,$00,$60,$E0,$00				; $CD03 ..j..`..
			defb $A4,$C0,$00,$4E,$00,$00,$0A,$00				; $CD0B ...N....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD13 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD1B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD23 ........
			defb $00,$12,$80,$00,$2B,$B0,$00,$1A				; $CD2B ....+...
			defb $B8,$00,$18,$37,$40,$1A,$B8,$00				; $CD33 ...7@...
			defb $2B,$B0,$00,$12,$80,$00,$00,$00				; $CD3B +.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD43 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD4B ........
			defb $00,$00,$00,$00,$00,$00,$00,$A0				; $CD53 ........
			defb $00,$04,$E0,$00,$0A,$4C,$00,$06				; $CD5B .....L..
			defb $0E,$00,$06,$AD,$D0,$06,$EE,$00				; $CD63 ........
			defb $0A,$AC,$00,$04,$00,$00,$00,$E0				; $CD6B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD73 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CD7B ........
			defb $00,$00,$00,$00,$00,$00,$00,$28				; $CD83 .......(
			defb $00,$01,$10,$00,$02,$83,$00,$01				; $CD8B ........
			defb $AB,$80,$01,$BB,$74,$01,$AB,$80				; $CD93 ....t...
			defb $02,$83,$00,$01,$10,$00,$00,$28				; $CD9B .......(
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CDA3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $CDAB ........
			defb $00,$00,$00,$00,$00,$00,$07,$00				; $CDB3 ........
			defb $00,$1E,$C0,$00,$1F,$CA,$00,$2F				; $CDBB ......./
			defb $DD,$00,$FE,$3A,$00,$FE,$FD,$00				; $CDC3 ...:....
			defb $78,$FA,$00,$02,$FA,$00,$F6,$78				; $CDCB x......x
			defb $00,$FF,$63,$00,$FF,$6E,$00,$BC				; $CDD3 ..c..n..
			defb $0B,$00,$73,$BD,$00,$0F,$BA,$00				; $CDDB ..s.....
			defb $3D,$B4,$00,$3A,$1C,$00,$01,$C0				; $CDE3 =..:....
			defb $00,$07,$B0,$00,$07,$F2,$80,$0B				; $CDEB ........
			defb $F7,$40,$3F,$8E,$80,$3F,$BF,$40				; $CDF3 .@?..?.@
			defb $1E,$3E,$80,$00,$BE,$80,$3D,$9E				; $CDFB .>....=.
			defb $00,$3F,$D8,$C0,$3F,$DB,$80,$2F				; $CE03 .?..?../
			defb $02,$C0,$1C,$EF,$40,$03,$EE,$80				; $CE0B ....@...
			defb $0F,$6D,$00,$0E,$87,$00,$00,$70				; $CE13 .m.....p
			defb $00,$01,$EC,$00,$01,$FC,$A0,$02				; $CE1B ........
			defb $FD,$D0,$0F,$E3,$A0,$0F,$EF,$D0				; $CE23 ........
			defb $07,$8F,$A0,$00,$2F,$A0,$0F,$67				; $CE2B ..../..g
			defb $80,$0F,$F6,$30,$0F,$F6,$E0,$0B				; $CE33 ...0....
			defb $C0,$B0,$07,$3B,$D0,$00,$FB,$A0				; $CE3B ...;....
			defb $03,$DB,$40,$03,$A1,$C0,$00,$1C				; $CE43 ..@.....
			defb $00,$00,$7B,$00,$00,$7F,$28,$00				; $CE4B ..{...(.
			defb $BF,$74,$03,$F8,$E8,$03,$FB,$F4				; $CE53 .t......
			defb $01,$E3,$E8,$00,$0B,$E8,$03,$D9				; $CE5B ........
			defb $E0,$03,$FD,$8C,$03,$FD,$B8,$02				; $CE63 ........
			defb $F0,$2C,$01,$CE,$F4,$00,$3E,$E8				; $CE6B .,....>.
			defb $00,$F6,$D0,$00,$E8,$70,$05,$00				; $CE73 .....p..
			defb $00,$1E,$C0,$00,$1B,$CA,$00,$2D				; $CE7B .......-
			defb $4D,$00,$B0,$2A,$00,$94,$2D,$00				; $CE83 M..*..-.
			defb $78,$1A,$00,$00,$0A,$00,$F0,$10				; $CE8B x.......
			defb $00,$A8,$03,$00,$D4,$2A,$00,$AC				; $CE93 .....*..
			defb $0B,$00,$72,$B5,$00,$0B,$8A,$00				; $CE9B ..r.....
			defb $35,$B4,$00,$2A,$1C,$00,$01,$40				; $CEA3 5..*...@
			defb $00,$07,$B0,$00,$06,$F2,$80,$0B				; $CEAB ........
			defb $53,$40,$2C,$0A,$80,$25,$0B,$40				; $CEB3 S@,..%.@
			defb $1E,$06,$80,$00,$02,$80,$3C,$04				; $CEBB ......<.
			defb $00,$2A,$00,$C0,$35,$0A,$80,$2B				; $CEC3 .*..5..+
			defb $02,$C0,$1C,$AD,$40,$02,$E2,$80				; $CECB ....@...
			defb $0D,$6D,$00,$0A,$87,$00,$00,$50				; $CED3 .m.....P
			defb $00,$01,$EC,$00,$01,$BC,$A0,$02				; $CEDB ........
			defb $D4,$D0,$0B,$02,$A0,$09,$42,$D0				; $CEE3 ......B.
			defb $07,$81,$A0,$00,$00,$A0,$0F,$01				; $CEEB ........
			defb $00,$0A,$80,$30,$0D,$42,$A0,$0A				; $CEF3 ...0.B..
			defb $C0,$B0,$07,$2B,$50,$00,$B8,$A0				; $CEFB ...+P...
			defb $03,$5B,$40,$02,$A1,$C0,$00,$14				; $CF03 .[@.....
			defb $00,$00,$7B,$00,$00,$6F,$28,$00				; $CF0B ..{..o(.
			defb $B5,$34,$02,$C0,$A8,$02,$50,$B4				; $CF13 .4....P.
			defb $01,$E0,$68,$00,$00,$28,$03,$C0				; $CF1B ..h..(..
			defb $40,$02,$A0,$0C,$03,$50,$A8,$02				; $CF23 @....P..
			defb $B0,$2C,$01,$CA,$D4,$00,$2E,$28				; $CF2B .,.....(
			defb $00,$D6,$D0,$00,$A8,$70,$07,$00				; $CF33 .....p..
			defb $00,$16,$C0,$00,$10,$0A,$00,$21				; $CF3B .......!
			defb $1D,$00,$E8,$0A,$00,$E0,$11,$00				; $CF43 ........
			defb $20,$02,$00,$00,$0A,$00,$C0,$00				; $CF4B  .......
			defb $00,$D0,$01,$00,$C0,$24,$00,$A4				; $CF53 .....$..
			defb $03,$00,$70,$05,$00,$08,$2A,$00				; $CF5B ..p...*.
			defb $39,$B4,$00,$3A,$1C,$00,$01,$C0				; $CF63 9..:....
			defb $00,$05,$B0,$00,$04,$02,$80,$08				; $CF6B ........
			defb $47,$40,$3A,$02,$80,$38,$04,$40				; $CF73 G@:..8.@
			defb $08,$00,$80,$00,$02,$80,$30,$00				; $CF7B ......0.
			defb $00,$34,$00,$40,$30,$09,$00,$29				; $CF83 .4.@0..)
			defb $00,$C0,$1C,$01,$40,$02,$0A,$80				; $CF8B ....@...
			defb $0E,$6D,$00,$0E,$87,$00,$00,$70				; $CF93 .m.....p
			defb $00,$01,$6C,$00,$01,$00,$A0,$02				; $CF9B ..l.....
			defb $11,$D0,$0E,$80,$A0,$0E,$01,$10				; $CFA3 ........
			defb $02,$00,$20,$00,$00,$A0,$0C,$00				; $CFAB .. .....
			defb $00,$0D,$00,$10,$0C,$02,$40,$0A				; $CFB3 ......@.
			defb $40,$30,$07,$00,$50,$00,$82,$A0				; $CFBB @0..P...
			defb $03,$9B,$40,$03,$A1,$C0,$00,$1C				; $CFC3 ..@.....
			defb $00,$00,$5B,$00,$00,$40,$28,$00				; $CFCB ..[..@(.
			defb $84,$74,$03,$A0,$28,$03,$80,$44				; $CFD3 .t..(..D
			defb $00,$80,$08,$00,$00,$28,$03,$00				; $CFDB .....(..
			defb $00,$03,$40,$04,$03,$00,$90,$02				; $CFE3 ..@.....
			defb $90,$0C,$01,$C0,$14,$00,$20,$A8				; $CFEB ...... .
			defb $00,$E6,$D0,$00,$E8,$70,$02,$00				; $CFF3 .....p..
			defb $00,$04,$40,$00,$20,$08,$00,$01				; $CFFB ..@. ...
			defb $02,$00,$08,$08,$00,$A0,$00,$00				; $D003 ........
			defb $00,$02,$00,$00,$00,$00,$80,$01				; $D00B ........
			defb $00,$00,$04,$00,$80,$01,$00,$00				; $D013 ........
			defb $00,$00,$50,$02,$00,$08,$20,$00				; $D01B ..P... .
			defb $20,$84,$00,$0A,$10,$00,$00,$80				; $D023  .......
			defb $00,$01,$10,$00,$08,$02,$00,$00				; $D02B ........
			defb $40,$80,$02,$02,$00,$28,$00,$00				; $D033 @....(..
			defb $00,$00,$80,$00,$00,$00,$20,$00				; $D03B ...... .
			defb $40,$00,$01,$00,$20,$00,$40,$00				; $D043 @... .@.
			defb $00,$00,$14,$00,$80,$02,$08,$00				; $D04B ........
			defb $08,$21,$00,$02,$84,$00,$00,$20				; $D053 .!..... 
			defb $00,$00,$44,$00,$02,$00,$80,$00				; $D05B ..D.....
			defb $10,$20,$00,$80,$80,$0A,$00,$00				; $D063 . ......
			defb $00,$00,$20,$00,$00,$00,$08,$00				; $D06B .. .....
			defb $10,$00,$00,$40,$08,$00,$10,$00				; $D073 ...@....
			defb $00,$00,$05,$00,$20,$00,$82,$00				; $D07B .... ...
			defb $02,$08,$40,$00,$A1,$00,$00,$08				; $D083 ..@.....
			defb $00,$00,$11,$00,$00,$80,$20,$00				; $D08B ...... .
			defb $04,$08,$00,$20,$20,$02,$80,$00				; $D093 ...  ...
			defb $00,$00,$08,$00,$00,$00,$02,$00				; $D09B ........
			defb $04,$00,$00,$10,$02,$00,$04,$00				; $D0A3 ........
			defb $00,$00,$01,$40,$08,$00,$20,$80				; $D0AB ...@.. .
			defb $00,$82,$10,$00,$28,$40,$03,$C0				; $D0B3 ....(@..
			defb $00,$0F,$D0,$00,$1F,$E8,$00,$3F				; $D0BB .......?
			defb $D4,$00,$7F,$AA,$00,$00,$00,$00				; $D0C3 ........
			defb $DC,$E7,$00,$DC,$E7,$00,$DC,$E7				; $D0CB ........
			defb $00,$00,$00,$00,$7F,$EA,$00,$FF				; $D0D3 ........
			defb $55,$00,$00,$00,$00,$7B,$DA,$00				; $D0DB U....{..
			defb $00,$00,$00,$19,$98,$00,$00,$F0				; $D0E3 ........
			defb $00,$03,$F4,$00,$07,$FA,$00,$0F				; $D0EB ........
			defb $F5,$00,$1F,$EA,$80,$00,$00,$00				; $D0F3 ........
			defb $2E,$73,$80,$2E,$73,$80,$2E,$73				; $D0FB .s..s..s
			defb $80,$00,$00,$00,$1F,$FA,$80,$3F				; $D103 .......?
			defb $D5,$40,$00,$00,$00,$1E,$F6,$80				; $D10B .@......
			defb $00,$00,$00,$0C,$63,$00,$00,$3C				; $D113 ....c..<
			defb $00,$00,$FD,$00,$01,$FE,$80,$03				; $D11B ........
			defb $FD,$40,$07,$FA,$A0,$00,$00,$00				; $D123 .@......
			defb $07,$39,$D0,$07,$39,$D0,$07,$39				; $D12B .9..9..9
			defb $D0,$00,$00,$00,$07,$FE,$A0,$0F				; $D133 ........
			defb $F5,$50,$00,$00,$00,$0F,$3C,$D0				; $D13B .P....<.
			defb $00,$00,$00,$06,$18,$60,$00,$0F				; $D143 .....`..
			defb $00,$00,$3F,$40,$00,$7F,$A0,$00				; $D14B ..?@....
			defb $FF,$50,$01,$FE,$A8,$00,$00,$00				; $D153 .P......
			defb $03,$9C,$EC,$03,$9C,$EC,$03,$9C				; $D15B ........
			defb $EC,$00,$00,$00,$01,$FF,$A8,$03				; $D163 ........
			defb $FD,$54,$00,$00,$00,$01,$EF,$68				; $D16B .T.....h
			defb $00,$00,$00,$00,$C6,$30,$05,$A0				; $D173 .....0..
			defb $00,$1D,$B8,$00,$3D,$B4,$00,$7D				; $D17B ....=..}
			defb $BA,$00,$7D,$BA,$00,$FD,$B5,$00				; $D183 ..}.....
			defb $FD,$BB,$00,$03,$C0,$00,$7F,$EA				; $D18B ........
			defb $00,$00,$00,$00,$7F,$EA,$00,$FF				; $D193 ........
			defb $F5,$00,$00,$00,$00,$3F,$D4,$00				; $D19B .....?..
			defb $0F,$D0,$00,$03,$40,$00,$01,$68				; $D1A3 ....@..h
			defb $00,$07,$6E,$00,$0F,$6D,$00,$1F				; $D1AB ..n..m..
			defb $6E,$80,$1F,$6E,$80,$3F,$6D,$40				; $D1B3 n..n.?m@
			defb $3F,$6E,$C0,$00,$F0,$00,$1F,$FA				; $D1BB ?n......
			defb $80,$00,$00,$00,$1F,$FA,$80,$3F				; $D1C3 .......?
			defb $FD,$40,$00,$00,$00,$0F,$F5,$00				; $D1CB .@......
			defb $03,$F4,$00,$00,$D0,$00,$00,$5A				; $D1D3 .......Z
			defb $00,$01,$DB,$80,$03,$DB,$40,$07				; $D1DB ......@.
			defb $DB,$A0,$07,$DB,$A0,$0F,$DB,$50				; $D1E3 .......P
			defb $0F,$DB,$B0,$00,$3C,$00,$07,$FE				; $D1EB ....<...
			defb $A0,$00,$00,$00,$07,$FE,$A0,$0F				; $D1F3 ........
			defb $FF,$50,$00,$00,$00,$03,$FD,$40				; $D1FB .P.....@
			defb $00,$FD,$00,$00,$34,$00,$00,$16				; $D203 ....4...
			defb $80,$00,$76,$E0,$00,$F6,$D0,$01				; $D20B ..v.....
			defb $F6,$E8,$01,$F6,$E8,$03,$F6,$D4				; $D213 ........
			defb $03,$F6,$EC,$00,$0F,$00,$01,$FF				; $D21B ........
			defb $A8,$00,$00,$00,$01,$FF,$A8,$03				; $D223 ........
			defb $FF,$D4,$00,$00,$00,$00,$FF,$50				; $D22B .......P
			defb $00,$3F,$40,$00,$0D,$00,$07,$A0				; $D233 .?@.....
			defb $00,$1F,$E8,$00,$3F,$D4,$00,$7F				; $D23B ....?...
			defb $EA,$00,$7F,$D6,$00,$FF,$E9,$00				; $D243 ........
			defb $FF,$D5,$00,$00,$00,$00,$2F,$D4				; $D24B ....../.
			defb $00,$4F,$D2,$00,$80,$01,$00,$87				; $D253 .O......
			defb $A1,$00,$80,$01,$00,$91,$89,$00				; $D25B ........
			defb $63,$C6,$00,$33,$CC,$00,$01,$E8				; $D263 c..3....
			defb $00,$07,$FA,$00,$0F,$F5,$00,$1F				; $D26B ........
			defb $FA,$80,$1F,$F5,$80,$3F,$FA,$40				; $D273 .....?.@
			defb $3F,$F5,$40,$00,$00,$00,$0B,$F5				; $D27B ?.@.....
			defb $00,$13,$F4,$80,$20,$00,$40,$21				; $D283 .... .@!
			defb $E8,$40,$20,$00,$40,$12,$64,$80				; $D28B .@ .@.d.
			defb $0C,$F3,$00,$06,$F6,$00,$00,$7A				; $D293 .......z
			defb $00,$01,$FE,$80,$03,$FD,$40,$07				; $D29B ......@.
			defb $FE,$A0,$07,$FD,$60,$0F,$FE,$90				; $D2A3 ....`...
			defb $0F,$FD,$50,$00,$00,$00,$02,$FD				; $D2AB ..P.....
			defb $40,$04,$FD,$20,$08,$00,$10,$08				; $D2B3 @.. ....
			defb $7A,$10,$04,$00,$20,$02,$5A,$40				; $D2BB z... .Z@
			defb $01,$BD,$80,$00,$FF,$00,$00,$1E				; $D2C3 ........
			defb $80,$00,$7F,$A0,$00,$FF,$50,$01				; $D2CB ......P.
			defb $FF,$A8,$01,$FF,$58,$03,$FF,$A4				; $D2D3 ....X...
			defb $03,$FF,$54,$00,$00,$00,$00,$BF				; $D2DB ..T.....
			defb $50,$01,$3F,$48,$02,$00,$04,$02				; $D2E3 P.?H....
			defb $1E,$84,$02,$00,$04,$01,$26,$48				; $D2EB ......&H
			defb $00,$CF,$30,$00,$6F,$60,$05,$A0				; $D2F3 ..0.o`..
			defb $00,$1D,$B8,$00,$3D,$B4,$00,$7D				; $D2FB ....=..}
			defb $BA,$00,$7D,$BA,$00,$FD,$B5,$00				; $D303 ..}.....
			defb $FD,$BB,$00,$03,$C0,$00,$3F,$FC				; $D30B ......?.
			defb $00,$3F,$FC,$00,$0E,$70,$00,$55				; $D313 .?...p.U
			defb $AA,$00,$BB,$DD,$00,$BB,$DA,$00				; $D31B ........
			defb $BB,$DD,$00,$51,$8A,$00,$01,$68				; $D323 ...Q...h
			defb $00,$07,$6E,$00,$0F,$6D,$00,$1F				; $D32B ..n..m..
			defb $6E,$80,$1F,$6E,$80,$3F,$6D,$40				; $D333 n..n.?m@
			defb $3F,$6E,$C0,$00,$F0,$00,$0F,$FF				; $D33B ?n......
			defb $00,$0F,$FF,$00,$03,$9C,$00,$15				; $D343 ........
			defb $6A,$80,$2E,$F7,$40,$2E,$F6,$80				; $D34B j...@...
			defb $2E,$F7,$40,$14,$62,$80,$00,$5A				; $D353 ..@.b..Z
			defb $00,$01,$DB,$80,$03,$DB,$40,$07				; $D35B ......@.
			defb $DB,$A0,$07,$DB,$A0,$0F,$DB,$50				; $D363 .......P
			defb $0F,$DB,$B0,$00,$3C,$00,$03,$FF				; $D36B ....<...
			defb $C0,$03,$FF,$C0,$00,$E7,$00,$05				; $D373 ........
			defb $5A,$A0,$0B,$BD,$D0,$0B,$BD,$A0				; $D37B Z.......
			defb $0B,$BD,$D0,$05,$18,$A0,$00,$16				; $D383 ........
			defb $80,$00,$76,$E0,$00,$F6,$D0,$01				; $D38B ..v.....
			defb $F6,$E8,$01,$F6,$E8,$03,$F6,$D4				; $D393 ........
			defb $03,$F6,$EC,$00,$0F,$00,$00,$FF				; $D39B ........
			defb $F0,$00,$FF,$F0,$00,$39,$C0,$01				; $D3A3 .....9..
			defb $56,$A8,$02,$EF,$74,$02,$EF,$68				; $D3AB V...t..h
			defb $02,$EF,$74,$01,$46,$28,$81,$02				; $D3B3 ..t.F(..
			defb $00,$6D,$6C,$00,$4B,$A4,$00,$1C				; $D3BB .ml.K...
			defb $70,$00,$7F,$FC,$00,$5E,$F4,$00				; $D3C3 p....^..
			defb $2D,$68,$00,$ED,$6E,$00,$2D,$68				; $D3CB -h..n.-h
			defb $00,$5E,$F4,$00,$7F,$FC,$00,$1C				; $D3D3 .^......
			defb $70,$00,$4B,$A4,$00,$6D,$6C,$00				; $D3DB p.K..ml.
			defb $81,$02,$00,$00,$00,$00,$20,$40				; $D3E3 ...... @
			defb $80,$1B,$5B,$00,$12,$E9,$00,$07				; $D3EB ..[.....
			defb $1C,$00,$1F,$FF,$00,$17,$BD,$00				; $D3F3 ........
			defb $0B,$5A,$00,$3B,$5B,$80,$0B,$5A				; $D3FB .Z.;[..Z
			defb $00,$17,$BD,$00,$1F,$FF,$00,$07				; $D403 ........
			defb $1C,$00,$12,$E9,$00,$1B,$5B,$00				; $D40B ......[.
			defb $20,$40,$80,$00,$00,$00,$08,$10				; $D413  @......
			defb $20,$06,$D6,$C0,$04,$BA,$40,$01				; $D41B  .....@.
			defb $C7,$00,$07,$FF,$C0,$05,$EF,$40				; $D423 .......@
			defb $02,$D6,$80,$0E,$D6,$E0,$02,$D6				; $D42B ........
			defb $80,$05,$EF,$40,$07,$FF,$C0,$01				; $D433 ...@....
			defb $C7,$00,$04,$BA,$40,$06,$D6,$C0				; $D43B ....@...
			defb $08,$10,$20,$00,$00,$00,$02,$04				; $D443 .. .....
			defb $08,$01,$B5,$B0,$01,$2E,$90,$00				; $D44B ........
			defb $71,$C0,$01,$FF,$F0,$01,$7B,$D0				; $D453 q.....{.
			defb $00,$B5,$A0,$03,$B5,$B8,$00,$B5				; $D45B ........
			defb $A0,$01,$7B,$D0,$01,$FF,$F0,$00				; $D463 ..{.....
			defb $71,$C0,$01,$2E,$90,$01,$B5,$B0				; $D46B q.......
			defb $02,$04,$08,$00,$00,$00,$00,$00				; $D473 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D47B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D483 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D48B ........
			defb $00,$00,$00,$00,$00,$00,$C1,$93				; $D493 ........
			defb $F5,$8B,$39,$A3,$93,$86,$43,$3E				; $D49B ..9...C>
			defb $8E,$78,$3C,$80,$F0,$C7,$C8,$CF				; $D4A3 .x<.....
			defb $1D,$DC,$4E,$99,$86,$1A,$1B,$59				; $D4AB ..N....Y
			defb $7B,$1C,$E1,$8F,$C9,$A3,$C9,$A3				; $D4B3 {.......
			defb $E5,$93,$70,$C7,$3A,$FE,$18,$38				; $D4BB ..p.:..8
			defb $9D,$02,$1C,$40,$F8,$1F,$E4,$BB				; $D4C3 ...@....
			defb $0E,$30,$26,$32,$4B,$1C,$1B,$AE				; $D4CB .0&2K...
			defb $39,$86,$F1,$A3,$C5,$83,$C5,$A3				; $D4D3 9.......
			defb $F1,$87,$38,$DE,$5A,$D8,$98,$62				; $D4DB ..8.Z..b
			defb $59,$70,$3B,$B8,$F3,$13,$E3,$0F				; $D4E3 Yp;.....
			defb $01,$3C,$9E,$70,$7C,$C2,$61,$C8				; $D4EB .<.p|.a.
			defb $C9,$9C,$D1,$8F,$C5,$A3,$C5,$A3				; $D4F3 ........
			defb $D1,$8F,$61,$9C,$75,$D9,$38,$D2				; $D4FB ..a.u.8.
			defb $4C,$61,$0C,$70,$DC,$27,$F8,$1F				; $D503 La.p.'..
			defb $02,$38,$40,$BA,$1C,$18,$7F,$5C				; $D50B .8@....\
			defb $E3,$0E,$C1,$A7,$C9,$83,$C1,$83				; $D513 ........
			defb $E5,$8F,$7B,$1C,$1B,$59,$86,$18				; $D51B ..{..Y..
			defb $4E,$99,$1D,$DC,$C8,$CF,$F0,$C7				; $D523 N.......
			defb $3C,$80,$8E,$79,$43,$3E,$93,$86				; $D52B <..yC>..
			defb $39,$93,$F1,$AB,$C1,$83,$00,$01				; $D533 9.......
			defb $00,$01,$00,$03,$00,$06,$00,$06				; $D53B ........
			defb $00,$00,$00,$1D,$00,$1D,$00,$00				; $D543 ........
			defb $00,$3B,$00,$77,$00,$77,$18,$77				; $D54B .;.w.w.w
			defb $00,$77,$34,$77,$7A,$3B,$00,$00				; $D553 .w4wz;..
			defb $B6,$AA,$7F,$FD,$EF,$FE,$FF,$AA				; $D55B ........
			defb $00,$00,$DF,$FF,$FF,$FF,$00,$00				; $D563 ........
			defb $BF,$F5,$FF,$FE,$7F,$FD,$FF,$FE				; $D56B ........
			defb $FF,$FD,$7F,$FA,$DF,$55,$80,$00				; $D573 .....U..
			defb $80,$00,$40,$00,$20,$00,$A0,$00				; $D57B ..@. ...
			defb $00,$00,$48,$00,$A8,$00,$00,$00				; $D583 ..H.....
			defb $54,$00,$AA,$00,$54,$00,$AA,$18				; $D58B T...T...
			defb $54,$00,$AA,$34,$54,$7A,$00,$00				; $D593 T..4Tz..
			defb $6D,$FA,$DF,$FD,$DB,$FA,$DF,$FD				; $D59B m.......
			defb $DB,$FA,$DF,$FD,$DB,$FA,$DF,$FD				; $D5A3 ........
			defb $DB,$FA,$DF,$FD,$DB,$FA,$DF,$FD				; $D5AB ........
			defb $DB,$FA,$DF,$F5,$6D,$AA,$30,$08				; $D5B3 ....m.0.
			defb $36,$64,$34,$48,$34,$44,$30,$08				; $D5BB 6d4H4D0.
			defb $36,$64,$34,$48,$34,$44,$30,$08				; $D5C3 6d4H4D0.
			defb $36,$64,$34,$48,$34,$44,$30,$08				; $D5CB 6d4H4D0.
			defb $36,$64,$34,$48,$34,$44,$6D,$FA				; $D5D3 6d4H4Dm.
			defb $DF,$FD,$DB,$FA,$DF,$FD,$DB,$FA				; $D5DB ........
			defb $DF,$FD,$DB,$FA,$DF,$FD,$DB,$FA				; $D5E3 ........
			defb $DF,$FD,$DB,$FA,$DF,$FD,$DB,$FA				; $D5EB ........
			defb $DF,$F5,$6D,$AA,$00,$00,$80,$00				; $D5F3 ..m.....
			defb $40,$00,$40,$00,$20,$C0,$20,$20				; $D5FB @.@. .  
			defb $20,$10,$60,$00,$60,$0F,$60,$30				; $D603  .`.`.`0
			defb $60,$66,$C0,$C6,$C0,$C6,$C1,$83				; $D60B `f......
			defb $C5,$A3,$D1,$8B,$C1,$83,$00,$00				; $D613 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D61B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D623 ........
			defb $00,$18,$08,$04,$10,$02,$62,$22				; $D62B ......b"
			defb $61,$03,$C9,$93,$C1,$83,$80,$00				; $D633 a.......
			defb $80,$00,$80,$00,$40,$00,$40,$00				; $D63B ....@.@.
			defb $60,$E0,$27,$80,$0C,$00,$18,$00				; $D643 `.'.....
			defb $30,$00,$36,$08,$63,$04,$6B,$06				; $D64B 0.6.c.k.
			defb $C1,$A3,$D5,$93,$C1,$83,$00,$00				; $D653 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D65B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $D663 ........
			defb $00,$00,$00,$00,$00,$01,$08,$81				; $D66B ........
			defb $85,$12,$91,$03,$C1,$A3,$00,$06				; $D673 ........
			defb $00,$3E,$00,$7C,$01,$BE,$03,$BC				; $D67B .>.|....
			defb $07,$DE,$07,$DC,$0F,$EA,$0F,$EC				; $D683 ........
			defb $17,$F2,$19,$F4,$3E,$FA,$3F,$3A				; $D68B ....>.?:
			defb $7F,$DC,$7F,$E4,$00,$00,$60,$00				; $D693 ......`.
			defb $7C,$00,$7E,$00,$7D,$80,$7D,$40				; $D69B |.~.}.}@
			defb $7B,$A0,$7B,$E0,$77,$D0,$77,$F0				; $D6A3 {.{.w.w.
			defb $6F,$E8,$6F,$98,$5F,$74,$5C,$F4				; $D6AB o.o._t\.
			defb $3B,$FA,$27,$EA,$00,$00,$00,$00				; $D6B3 ;.'.....
			defb $FF,$F8,$FF,$E6,$7F,$9C,$7E,$7C				; $D6BB ......~|
			defb $79,$FA,$27,$F6,$1F,$F4,$17,$EE				; $D6C3 y.'.....
			defb $0B,$DC,$06,$DA,$01,$FC,$00,$66				; $D6CB .......f
			defb $00,$18,$00,$06,$00,$00,$00,$00				; $D6D3 ........
			defb $1F,$FF,$67,$FF,$39,$FE,$3E,$7E				; $D6DB ..g.9.>~
			defb $5F,$9E,$6F,$E4,$6F,$F8,$77,$E8				; $D6E3 _.o.o.w.
			defb $7B,$D0,$7B,$60,$3D,$80,$66,$00				; $D6EB {.{`=.f.
			defb $18,$00,$60,$00,$00,$00,$00,$00				; $D6F3 ..`.....
			defb $FF,$FF,$55,$55,$00,$00,$FF,$FF				; $D6FB ..UU....
			defb $BF,$FD,$E0,$07,$E5,$57,$E2,$AF				; $D703 .....W..
			defb $E5,$57,$BF,$FD,$FF,$FF,$00,$00				; $D70B .W......
			defb $FF,$FF,$55,$55,$00,$00,$4F,$F4				; $D713 ..UU..O.
			defb $6B,$D6,$4F,$F4,$6C,$36,$4C,$34				; $D71B k.O.l6L4
			defb $6D,$76,$4C,$B4,$6D,$76,$4C,$B4				; $D723 mvL.mvL.
			defb $6D,$76,$4C,$B4,$6D,$76,$4C,$B4				; $D72B mvL.mvL.
			defb $6F,$F6,$4B,$D4,$6F,$F6,$07,$80				; $D733 o.K.o...
			defb $1F,$7F,$3F,$7F,$7F,$6F,$7F,$7F				; $D73B ..?..o..
			defb $FF,$7F,$FF,$7F,$FF,$7F,$FF,$7F				; $D743 ........
			defb $FF,$7F,$FF,$7F,$7F,$7F,$7F,$6F				; $D74B .......o
			defb $3F,$2A,$1F,$55,$07,$80,$01,$E0				; $D753 ?*.U....
			defb $FA,$F8,$FC,$FC,$EA,$FE,$FC,$FE				; $D75B ........
			defb $FA,$FD,$F4,$FE,$FA,$FD,$F4,$FE				; $D763 ........
			defb $FA,$FD,$F4,$FD,$EA,$F6,$54,$EA				; $D76B ......T.
			defb $AA,$F4,$54,$C8,$01,$A0,$00,$00				; $D773 ..T.....
			defb $00,$00,$00,$00,$00,$00,$41,$82				; $D77B ......A.
			defb $07,$E0,$60,$06,$6F,$F6,$6F,$F6				; $D783 ..`.o.o.
			defb $60,$06,$07,$E0,$41,$82,$00,$00				; $D78B `...A...
			defb $00,$00,$00,$00,$00,$00,$07,$E0				; $D793 ........
			defb $1F,$F8,$3C,$3C,$70,$0E,$0C,$30				; $D79B ..<<p..0
			defb $EA,$57,$C6,$E2,$C1,$C3,$C3,$82				; $D7A3 .W......
			defb $C7,$63,$EA,$57,$0C,$30,$70,$0E				; $D7AB .c.W.0p.
			defb $3C,$34,$1F,$E8,$06,$A0,$00,$00				; $D7B3 <4......
			defb $00,$FE,$01,$02,$02,$78,$7A,$7A				; $D7BB .....xzz
			defb $4B,$00,$73,$FE,$73,$FC,$73,$FA				; $D7C3 K.s.s.s.
			defb $6B,$D4,$43,$02,$6A,$78,$02,$7A				; $D7CB k.C.jx.z
			defb $01,$00,$00,$AA,$00,$00,$00,$00				; $D7D3 ........
			defb $FF,$FF,$00,$00,$33,$33,$00,$00				; $D7DB ....33..
			defb $FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF				; $D7E3 ........
			defb $AA,$AA,$55,$55,$00,$00,$33,$33				; $D7EB ..UU..33
			defb $00,$00,$AA,$AA,$00,$00,$00,$00				; $D7F3 ........
			defb $7F,$00,$40,$80,$5E,$40,$5E,$5E				; $D7FB ..@.^@^^
			defb $40,$D2,$7E,$9C,$7F,$5A,$7E,$9C				; $D803 @.~..Z~.
			defb $75,$5A,$40,$90,$5E,$5A,$5E,$40				; $D80B uZ@.^Z^@
			defb $40,$80,$55,$00,$00,$00,$0F,$D0				; $D813 @.U.....
			defb $2F,$E4,$2F,$D4,$2F,$E4,$0F,$D0				; $D81B /././...
			defb $00,$00,$07,$A0,$07,$C0,$07,$A0				; $D823 ........
			defb $00,$00,$01,$80,$01,$80,$00,$00				; $D82B ........
			defb $03,$40,$03,$80,$03,$40,$0F,$D0				; $D833 .@...@..
			defb $6F,$E6,$6F,$D6,$6F,$E6,$0F,$D0				; $D83B o.o.o...
			defb $00,$00,$07,$A0,$07,$C0,$07,$A0				; $D843 ........
			defb $00,$00,$01,$80,$00,$00,$03,$40				; $D84B .......@
			defb $03,$80,$03,$40,$00,$00,$0F,$D0				; $D853 ...@....
			defb $EF,$E7,$EF,$D7,$EF,$E7,$0F,$D0				; $D85B ........
			defb $00,$00,$07,$A0,$07,$C0,$07,$A0				; $D863 ........
			defb $00,$00,$00,$00,$03,$40,$03,$80				; $D86B .....@..
			defb $03,$40,$00,$00,$00,$00,$03,$40				; $D873 .@.....@
			defb $03,$80,$03,$40,$00,$00,$01,$80				; $D87B ...@....
			defb $01,$80,$00,$00,$07,$A0,$07,$C0				; $D883 ........
			defb $07,$A0,$00,$00,$0F,$D0,$2F,$E4				; $D88B ....../.
			defb $2F,$D4,$2F,$E4,$0F,$D0,$00,$00				; $D893 /./.....
			defb $03,$40,$03,$80,$03,$40,$00,$00				; $D89B .@...@..
			defb $01,$80,$00,$00,$07,$A0,$07,$C0				; $D8A3 ........
			defb $07,$A0,$00,$00,$0F,$D0,$6F,$E6				; $D8AB ......o.
			defb $6F,$D6,$6F,$E6,$0F,$D0,$00,$00				; $D8B3 o.o.....
			defb $00,$00,$03,$40,$03,$80,$03,$40				; $D8BB ...@...@
			defb $00,$00,$00,$00,$07,$A0,$07,$C0				; $D8C3 ........
			defb $07,$A0,$00,$00,$0F,$D0,$EF,$E7				; $D8CB ........
			defb $EF,$D7,$EF,$E7,$0F,$D0,$01,$E0				; $D8D3 ........
			defb $02,$D0,$0F,$70,$1F,$64,$1C,$52				; $D8DB ...p.d.R
			defb $33,$D2,$31,$A1,$64,$01,$64,$01				; $D8E3 3.1.d.d.
			defb $44,$01,$42,$02,$43,$02,$81,$86				; $D8EB D.B.C...
			defb $00,$CC,$00,$58,$00,$38,$00,$01				; $D8F3 ...X.8..
			defb $00,$01,$00,$02,$00,$0C,$00,$78				; $D8FB .......x
			defb $17,$C0,$31,$00,$39,$00,$14,$00				; $D903 ..1.9...
			defb $2A,$00,$14,$00,$01,$00,$10,$80				; $D90B *.......
			defb $08,$80,$08,$80,$04,$40,$00,$68				; $D913 .....@.h
			defb $00,$50,$00,$E0,$00,$D0,$01,$E0				; $D91B .P......
			defb $01,$D0,$02,$A8,$03,$51,$07,$AB				; $D923 .....Q..
			defb $07,$D3,$0B,$E9,$1D,$D5,$1D,$A8				; $D92B ........
			defb $3A,$EB,$66,$D7,$C1,$03,$04,$40				; $D933 :.f....@
			defb $04,$40,$0C,$40,$08,$80,$19,$80				; $D93B .@.@....
			defb $33,$00,$C6,$00,$AA,$00,$D4,$00				; $D943 3.......
			defb $A8,$00,$DA,$00,$A7,$00,$9D,$00				; $D94B ........
			defb $3E,$80,$C3,$20,$78,$55,$06,$5A				; $D953 >.. xU.Z
			defb $19,$6D,$21,$6D,$41,$6D,$43,$6D				; $D95B .m!mAmCm
			defb $86,$ED,$DD,$DD,$73,$DB,$0F,$3B				; $D963 ....s..;
			defb $FC,$F7,$63,$EE,$1F,$9E,$FC,$7D				; $D96B ..c....}
			defb $33,$FB,$8F,$E7,$7F,$1E,$00,$00				; $D973 3.......
			defb $00,$00,$80,$00,$A0,$00,$A0,$00				; $D97B ........
			defb $A0,$00,$B0,$00,$B0,$00,$70,$00				; $D983 ......p.
			defb $68,$00,$68,$00,$E8,$00,$D0,$00				; $D98B h.h.....
			defb $D8,$00,$30,$00,$D0,$00,$00,$FD				; $D993 ..0.....
			defb $1F,$E3,$07,$9F,$00,$74,$00,$03				; $D99B .....t..
			defb $00,$3F,$00,$3F,$00,$1E,$00,$0F				; $D9A3 .?.?....
			defb $00,$00,$00,$1F,$00,$1F,$00,$0F				; $D9AB ........
			defb $00,$73,$01,$7C,$DF,$AD,$A0,$00				; $D9B3 .s.|....
			defb $40,$00,$80,$00,$40,$00,$80,$00				; $D9BB @...@...
			defb $40,$00,$80,$00,$80,$00,$21,$F0				; $D9C3 @.....!.
			defb $D3,$0C,$A3,$86,$D5,$FC,$A2,$D0				; $D9CB ........
			defb $95,$20,$2A,$C8,$55,$B7,$7B,$FF				; $D9D3 . *.U.{.
			defb $FD,$BF,$FE,$FF,$FF,$7F,$FF,$B7				; $D9DB ........
			defb $FF,$DF,$7F,$EF,$00,$07,$7F,$AA				; $D9E3 ........
			defb $FF,$F5,$FF,$FA,$7F,$F4,$77,$FA				; $D9EB ......w.
			defb $3B,$F4,$1C,$D8,$07,$E0,$F1,$54				; $D9F3 ;......T
			defb $D8,$AA,$FC,$55,$FE,$2A,$FB,$15				; $D9FB ...U.*..
			defb $FF,$8A,$81,$C4,$60,$E0,$C0,$70				; $DA03 ....`..p
			defb $90,$38,$38,$5C,$78,$EE,$35,$67				; $DA0B .88\x.5g
			defb $0E,$A2,$0F,$40,$0F,$A0,$17,$D0				; $DA13 ...@....
			defb $3B,$D0,$1D,$E8,$00,$D8,$00,$34				; $DA1B ;......4
			defb $00,$0E,$00,$06,$00,$01,$00,$00				; $DA23 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DA2B ........
			defb $00,$00,$00,$00,$00,$00,$FF,$FE				; $DA33 ........
			defb $80,$00,$BB,$BA,$A2,$20,$A2,$22				; $DA3B ..... ."
			defb $80,$00,$BB,$BA,$A2,$20,$A2,$22				; $DA43 ..... ."
			defb $80,$00,$BB,$BA,$A2,$20,$A2,$22				; $DA4B ..... ."
			defb $80,$00,$AA,$AA,$00,$00,$FF,$FE				; $DA53 ........
			defb $80,$00,$BF,$FA,$A0,$00,$AA,$AA				; $DA5B ........
			defb $A5,$50,$AA,$AA,$A5,$50,$AA,$AA				; $DA63 .P...P..
			defb $A5,$50,$AA,$AA,$A5,$50,$AA,$AA				; $DA6B .P...P..
			defb $80,$00,$AA,$AA,$00,$00,$DF,$AD				; $DA73 ........
			defb $01,$7C,$00,$73,$00,$0F,$00,$1F				; $DA7B .|.s....
			defb $00,$1F,$00,$00,$00,$0F,$00,$1E				; $DA83 ........
			defb $00,$3F,$00,$3F,$00,$03,$00,$74				; $DA8B .?.?...t
			defb $07,$9F,$1F,$E3,$00,$FD,$55,$B7				; $DA93 ......U.
			defb $2A,$C8,$95,$20,$A2,$D0,$D5,$FC				; $DA9B *.. ....
			defb $A3,$86,$D3,$0C,$21,$F0,$80,$00				; $DAA3 ....!...
			defb $80,$00,$40,$00,$80,$00,$40,$00				; $DAAB ..@...@.
			defb $80,$00,$40,$00,$A0,$00,$7F,$1E				; $DAB3 ..@.....
			defb $8F,$E7,$33,$FB,$FC,$7D,$1F,$9E				; $DABB ..3..}..
			defb $63,$EE,$FC,$F7,$0F,$3B,$73,$DB				; $DAC3 c....;s.
			defb $DD,$DD,$86,$ED,$43,$6D,$41,$6D				; $DACB ....CmAm
			defb $21,$6D,$19,$6D,$06,$5A,$D0,$00				; $DAD3 !m.m.Z..
			defb $30,$00,$D8,$00,$D0,$00,$E8,$00				; $DADB 0.......
			defb $68,$00,$68,$00,$70,$00,$B0,$00				; $DAE3 h.h.p...
			defb $B0,$00,$A0,$00,$A0,$00,$A0,$00				; $DAEB ........
			defb $80,$00,$00,$00,$00,$00,$C1,$83				; $DAF3 ........
			defb $D1,$8B,$C5,$A3,$C1,$83,$C0,$C6				; $DAFB ........
			defb $C0,$C6,$60,$66,$60,$30,$60,$0F				; $DB03 ..`f`0`.
			defb $60,$00,$20,$10,$20,$20,$20,$C0				; $DB0B `. .   .
			defb $40,$00,$40,$00,$80,$00,$C1,$83				; $DB13 @.@.....
			defb $C9,$93,$61,$03,$62,$22,$10,$02				; $DB1B ..a.b"..
			defb $08,$04,$00,$18,$00,$00,$00,$00				; $DB23 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DB2B ........
			defb $00,$00,$00,$00,$00,$00,$C1,$83				; $DB33 ........
			defb $D5,$93,$C1,$A3,$6B,$06,$63,$04				; $DB3B ....k.c.
			defb $36,$08,$30,$00,$18,$00,$0C,$00				; $DB43 6.0.....
			defb $27,$80,$60,$E0,$40,$00,$40,$00				; $DB4B '.`.@.@.
			defb $80,$00,$80,$00,$80,$00,$C1,$A3				; $DB53 ........
			defb $91,$03,$85,$12,$08,$81,$00,$01				; $DB5B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DB63 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DB6B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DB73 ........
			defb $00,$00,$00,$00,$63,$8E,$E6,$DB				; $DB7B ....c...
			defb $66,$DB,$66,$DB,$66,$DB,$66,$DB				; $DB83 f.f.f.f.
			defb $66,$DB,$66,$DB,$66,$DB,$F3,$8E				; $DB8B f.f.f...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DB93 ........
			defb $00,$00,$00,$00,$63,$CE,$F3,$1B				; $DB9B ....c...
			defb $B3,$1B,$33,$1B,$33,$9B,$60,$DB				; $DBA3 ..3.3.`.
			defb $C0,$DB,$C0,$DB,$C2,$DB,$F1,$8E				; $DBAB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $DBB3 ........
			defb $00,$00,$00,$00,$F3,$8E,$C6,$DB				; $DBBB ........
			defb $C6,$DB,$C6,$DB,$E6,$DB,$36,$DB				; $DBC3 ......6.
			defb $36,$DB,$36,$DB,$B6,$DB,$E3,$8E				; $DBCB 6.6.....
			defb $00,$00,$00,$00,$00,$00,$6F,$DA				; $DBD3 ......o.
			defb $6F,$F4,$6F,$DA,$6F,$F4,$6F,$DA				; $DBDB o.o.o.o.
			defb $6F,$F4,$6F,$DA,$6F,$F4,$6F,$DA				; $DBE3 o.o.o.o.
			defb $6F,$F4,$6F,$DA,$6F,$F4,$6F,$DA				; $DBEB o.o.o.o.
			defb $6F,$F4,$6F,$DA,$6F,$F4,$80,$01				; $DBF3 o.o.o...
			defb $D5,$55,$DA,$AB,$00,$00,$6D,$5A				; $DBFB .U....mZ
			defb $6E,$B4,$6F,$5A,$6E,$B4,$6F,$5A				; $DC03 n.oZn.oZ
			defb $6F,$B4,$6F,$5A,$6F,$B4,$6F,$DA				; $DC0B o.oZo.o.
			defb $6F,$B4,$6F,$DA,$6F,$F4,$6F,$F4				; $DC13 o.o.o.o.
			defb $6F,$DA,$6F,$B4,$6F,$DA,$6F,$B4				; $DC1B o.o.o.o.
			defb $6F,$5A,$6F,$B4,$6F,$5A,$6E,$B4				; $DC23 oZo.oZn.
			defb $6F,$5A,$6E,$B4,$6D,$5A,$00,$00				; $DC2B oZn.mZ..
			defb $DA,$AB,$D5,$55,$80,$01,$35,$54				; $DC33 ...U..5T
			defb $7F,$EA,$00,$00,$DC,$E7,$DC,$E7				; $DC3B ........
			defb $DC,$E7,$00,$00,$6F,$EA,$6F,$F6				; $DC43 ....o.o.
			defb $00,$00,$2F,$D4,$37,$EC,$5B,$DA				; $DC4B ../.7.[.
			defb $4F,$56,$33,$CC,$80,$01,$35,$54				; $DC53 OV3...5T
			defb $7F,$EA,$00,$00,$B9,$CE,$B9,$CE				; $DC5B ........
			defb $B9,$CE,$00,$00,$6F,$EA,$6F,$F6				; $DC63 ....o.o.
			defb $00,$00,$2F,$D4,$37,$EC,$5B,$DA				; $DC6B ../.7.[.
			defb $4F,$56,$33,$CC,$80,$01,$35,$54				; $DC73 OV3...5T
			defb $7F,$EA,$00,$00,$73,$9D,$73,$9D				; $DC7B ....s.s.
			defb $73,$9D,$00,$00,$6F,$EA,$6F,$F6				; $DC83 s...o.o.
			defb $00,$00,$2F,$D4,$37,$EC,$5B,$DA				; $DC8B ../.7.[.
			defb $4F,$56,$33,$CC,$80,$01,$35,$54				; $DC93 OV3...5T
			defb $7F,$EA,$00,$00,$E7,$3B,$E7,$3B				; $DC9B .....;.;
			defb $E7,$3B,$00,$00,$6F,$EA,$6F,$F6				; $DCA3 .;..o.o.
			defb $00,$00,$2F,$D4,$37,$EC,$5B,$DA				; $DCAB ../.7.[.
			defb $4F,$56,$33,$CC,$80,$01,$80,$01				; $DCB3 OV3.....
			defb $33,$CC,$4F,$56,$5B,$DA,$37,$EC				; $DCBB 3.OV[.7.
			defb $2F,$D4,$00,$00,$6F,$F6,$6F,$EA				; $DCC3 /...o.o.
			defb $00,$00,$E7,$3B,$E7,$3B,$E7,$3B				; $DCCB ...;.;.;
			defb $00,$00,$7F,$EA,$35,$54,$80,$01				; $DCD3 ....5T..
			defb $33,$CC,$4F,$56,$5B,$DA,$37,$EC				; $DCDB 3.OV[.7.
			defb $2F,$D4,$00,$00,$6F,$F6,$6F,$EA				; $DCE3 /...o.o.
			defb $00,$00,$73,$9D,$73,$9D,$73,$9D				; $DCEB ..s.s.s.
			defb $00,$00,$7F,$EA,$35,$54,$80,$01				; $DCF3 ....5T..
			defb $33,$CC,$4F,$56,$5B,$DA,$37,$EC				; $DCFB 3.OV[.7.
			defb $2F,$D4,$00,$00,$6F,$F6,$6F,$EA				; $DD03 /...o.o.
			defb $00,$00,$B9,$CE,$B9,$CE,$B9,$CE				; $DD0B ........
			defb $00,$00,$7F,$EA,$35,$54,$80,$01				; $DD13 ....5T..
			defb $33,$CC,$4F,$56,$5B,$DA,$37,$EC				; $DD1B 3.OV[.7.
			defb $2F,$D4,$00,$00,$6F,$F6,$6F,$EA				; $DD23 /...o.o.
			defb $00,$00,$DC,$E7,$DC,$E7,$DC,$E7				; $DD2B ........
			defb $00,$00,$7F,$EA,$35,$54,$01,$00				; $DD33 ....5T..
			defb $00,$00,$03,$80,$03,$80,$00,$00				; $DD3B ........
			defb $03,$80,$07,$40,$07,$80,$07,$40				; $DD43 ...@...@
			defb $07,$80,$07,$40,$10,$10,$17,$D0				; $DD4B ...@....
			defb $3B,$B8,$7B,$BC,$79,$3C,$79,$3C				; $DD53 ;.{.y<y<
			defb $7B,$BC,$3B,$B8,$17,$D0,$10,$10				; $DD5B {.;.....
			defb $07,$40,$07,$80,$07,$40,$07,$80				; $DD63 .@...@..
			defb $07,$40,$03,$80,$00,$00,$03,$80				; $DD6B .@......
			defb $03,$80,$00,$00,$01,$00,$00,$00				; $DD73 ........
			defb $0F,$FE,$3F,$7E,$3F,$7E,$7F,$BE				; $DD7B ..?~?~..
			defb $7F,$DC,$7F,$DA,$7F,$EC,$1F,$E2				; $DD83 ........
			defb $67,$F4,$79,$FA,$7E,$78,$7F,$94				; $DD8B g.y.~x..
			defb $7F,$A6,$7D,$58,$00,$00,$00,$00				; $DD93 ..}X....
			defb $7E,$F0,$7E,$FC,$7D,$FC,$7D,$FE				; $DD9B ~.~.}.}.
			defb $7B,$FC,$7B,$FA,$77,$FC,$77,$F2				; $DDA3 {.{.w.w.
			defb $6F,$EC,$6F,$9A,$5E,$74,$5C,$FA				; $DDAB o.o.^t\.
			defb $33,$F4,$27,$AA,$00,$00,$00,$00				; $DDB3 3.'.....
			defb $55,$E4,$2F,$CC,$5F,$3A,$2E,$7A				; $DDBB U./._:.z
			defb $59,$F6,$37,$F6,$4F,$EC,$3F,$EE				; $DDC3 Y.7.O.?.
			defb $5F,$DC,$3F,$DA,$7F,$BC,$3F,$BA				; $DDCB _.?...?.
			defb $3F,$74,$0F,$6A,$00,$00,$00,$00				; $DDD3 ?t.j....
			defb $1F,$FE,$67,$FE,$39,$FE,$1E,$7E				; $DDDB ..g.9..~
			defb $5F,$9C,$6F,$E6,$67,$F8,$77,$FE				; $DDE3 _.o.g.w.
			defb $7B,$FC,$7B,$FA,$7D,$FC,$7E,$FA				; $DDEB {.{.}.~.
			defb $7E,$F4,$7F,$68,$00,$00,$00,$00				; $DDF3 ~..h....
			defb $DF,$FF,$DF,$FF,$DF,$FF,$00,$00				; $DDFB ........
			defb $6F,$FF,$6F,$FF,$6F,$FF,$00,$00				; $DE03 o.o.o...
			defb $1B,$FF,$1B,$FF,$1B,$FF,$1B,$FF				; $DE0B ........
			defb $1B,$FF,$1B,$FF,$1B,$FF,$00,$00				; $DE13 ........
			defb $FD,$55,$FE,$AA,$FD,$55,$00,$00				; $DE1B .U...U..
			defb $FE,$AA,$FD,$54,$FE,$AA,$00,$00				; $DE23 ...T....
			defb $FE,$A8,$FD,$50,$FE,$A8,$FD,$50				; $DE2B ...P...P
			defb $FA,$A8,$F5,$50,$AA,$A8,$00,$00				; $DE33 ...P....
			defb $EF,$FF,$EE,$FF,$EF,$FF,$76,$FF				; $DE3B ......v.
			defb $77,$FF,$3B,$7F,$00,$00,$0D,$DF				; $DE43 w.;.....
			defb $0D,$FF,$0D,$DF,$0D,$FF,$0D,$DF				; $DE4B ........
			defb $0D,$FF,$0D,$DF,$0D,$FF,$00,$00				; $DE53 ........
			defb $F5,$55,$FA,$AA,$F5,$55,$FA,$AA				; $DE5B .U...U..
			defb $F5,$56,$EA,$A8,$00,$00,$55,$50				; $DE63 .V....UP
			defb $AA,$A0,$D5,$50,$EA,$A0,$D5,$50				; $DE6B ...P...P
			defb $EA,$A0,$D5,$50,$EA,$A0,$0D,$DF				; $DE73 ...P....
			defb $0D,$FF,$0D,$DF,$0D,$FF,$0D,$DF				; $DE7B ........
			defb $0D,$FF,$0D,$DF,$00,$00,$37,$7F				; $DE83 ......7.
			defb $37,$FF,$37,$7F,$00,$00,$EE,$FF				; $DE8B 7.7.....
			defb $EF,$FF,$EE,$FF,$00,$00,$D5,$50				; $DE93 .......P
			defb $EA,$A0,$D5,$50,$EA,$A0,$D5,$50				; $DE9B ...P...P
			defb $AA,$A0,$55,$50,$00,$00,$D5,$54				; $DEA3 ..UP...T
			defb $EA,$A8,$F5,$54,$00,$00,$F5,$55				; $DEAB ...T...U
			defb $FA,$AA,$F5,$55,$00,$00,$FF,$7F				; $DEB3 ...U....
			defb $FF,$5F,$FF,$7F,$FF,$7F,$7F,$7F				; $DEBB ._......
			defb $7F,$7F,$7F,$7F,$3F,$7F,$3F,$7F				; $DEC3 ....?.?.
			defb $1F,$7F,$0F,$7F,$07,$7F,$03,$7F				; $DECB ........
			defb $01,$5F,$00,$7F,$00,$0F,$FE,$AB				; $DED3 ._......
			defb $FA,$D5,$FE,$AA,$FE,$D5,$FE,$AA				; $DEDB ........
			defb $FE,$D4,$FE,$AA,$FE,$D4,$FE,$94				; $DEE3 ........
			defb $FE,$A8,$FE,$D0,$FE,$A0,$FE,$C0				; $DEEB ........
			defb $FA,$80,$FE,$00,$F0,$00,$FF,$54				; $DEF3 .......T
			defb $FF,$2B,$FF,$57,$FF,$00,$7F,$7F				; $DEFB .+.W....
			defb $7F,$5F,$7F,$7F,$3F,$7F,$3F,$7F				; $DF03 ._..?.?.
			defb $1F,$7F,$0F,$7F,$07,$7F,$03,$7F				; $DF0B ........
			defb $01,$7F,$00,$7F,$00,$0F,$14,$AB				; $DF13 ........
			defb $CA,$D5,$E4,$AA,$00,$D5,$FE,$AA				; $DF1B ........
			defb $FA,$D4,$FE,$AA,$FE,$D4,$FE,$94				; $DF23 ........
			defb $FE,$A8,$FE,$D0,$FE,$A0,$FE,$C0				; $DF2B ........
			defb $FE,$80,$FE,$00,$F0,$00,$FF,$54				; $DF33 .......T
			defb $FF,$2B,$FF,$57,$FF,$24,$7F,$48				; $DF3B .+.W.$.H
			defb $7F,$28,$7F,$48,$3F,$00,$3F,$7F				; $DF43 .(.H?.?.
			defb $1F,$5F,$0F,$7F,$07,$7F,$03,$7F				; $DF4B ._......
			defb $01,$7F,$00,$7F,$00,$0F,$14,$AB				; $DF53 ........
			defb $CA,$D5,$E4,$AA,$22,$D5,$10,$AA				; $DF5B ...."...
			defb $12,$D4,$10,$AA,$00,$D4,$FE,$94				; $DF63 ........
			defb $FA,$A8,$FE,$D0,$FE,$A0,$FE,$C0				; $DF6B ........
			defb $FE,$80,$FE,$00,$F0,$00,$FF,$54				; $DF73 .......T
			defb $FF,$2B,$FF,$57,$FF,$24,$7F,$48				; $DF7B .+.W.$.H
			defb $7F,$28,$7F,$48,$3F,$28,$3F,$44				; $DF83 .(.H?(?D
			defb $1F,$23,$0F,$54,$07,$00,$03,$7F				; $DF8B .#.T....
			defb $01,$5F,$00,$7F,$00,$0F,$14,$AB				; $DF93 ._......
			defb $CA,$D5,$E4,$AA,$22,$D5,$10,$AA				; $DF9B ...."...
			defb $12,$D4,$10,$AA,$12,$D4,$20,$94				; $DFA3 ...... .
			defb $C2,$A8,$04,$D0,$00,$A0,$FE,$C0				; $DFAB ........
			defb $FA,$80,$FE,$00,$F0,$00,$FF,$FF				; $DFB3 ........
			defb $7F,$FF,$3F,$FF,$1F,$FF,$0F,$FF				; $DFBB ..?.....
			defb $07,$E0,$03,$F0,$01,$F8,$00,$FC				; $DFC3 ........
			defb $00,$7E,$00,$3F,$00,$1F,$00,$0F				; $DFCB .~.?....
			defb $00,$07,$00,$03,$00,$01,$FF,$DF				; $DFD3 ........
			defb $FF,$BF,$FF,$1F,$FE,$0F,$FC,$0F				; $DFDB ........
			defb $00,$0F,$00,$0F,$00,$0F,$00,$0F				; $DFE3 ........
			defb $00,$0F,$00,$0F,$80,$0F,$C0,$0F				; $DFEB ........
			defb $E0,$0F,$F0,$0F,$F8,$0F,$F0,$3F				; $DFF3 .......?
			defb $E0,$7F,$C0,$FF,$81,$F8,$83,$F0				; $DFFB ........
			defb $87,$E0,$8F,$FF,$9F,$FF,$BF,$FF				; $E003 ........
			defb $BE,$0F,$BC,$1F,$B8,$3F,$BF,$FF				; $E00B .....?..
			defb $BF,$FF,$BF,$FF,$BF,$FE,$F8,$01				; $E013 ........
			defb $FC,$03,$FE,$07,$3F,$0F,$7E,$1F				; $E01B ....?.~.
			defb $FC,$3F,$F8,$7F,$F0,$FF,$C1,$FF				; $E023 .?......
			defb $E3,$F0,$F7,$E0,$EF,$C0,$DF,$FF				; $E02B ........
			defb $BF,$FF,$7F,$FF,$FF,$FF,$FF,$FB				; $E033 ........
			defb $FF,$F7,$FF,$EF,$FF,$DF,$80,$3F				; $E03B .......?
			defb $00,$7E,$FE,$FF,$FD,$FF,$FB,$FF				; $E043 .~......
			defb $07,$FF,$0F,$CF,$1F,$87,$FF,$03				; $E04B ........
			defb $FE,$01,$FC,$00,$F8,$00,$FF,$80				; $E053 ........
			defb $FF,$C1,$FF,$E3,$83,$F7,$07,$EF				; $E05B ........
			defb $0F,$DF,$FF,$BF,$FF,$7E,$FE,$FC				; $E063 .....~..
			defb $81,$F8,$C3,$F0,$E7,$E0,$FF,$C1				; $E06B ........
			defb $FF,$83,$FF,$07,$7E,$0F,$FF,$E0				; $E073 ....~...
			defb $FF,$F0,$FF,$F8,$E0,$FD,$C1,$FB				; $E07B ........
			defb $83,$F7,$07,$EF,$0F,$DF,$1F,$BF				; $E083 ........
			defb $3F,$7E,$7E,$FC,$FD,$F8,$FB,$FF				; $E08B ?~~.....
			defb $F1,$FF,$E0,$FF,$C0,$7F,$3F,$F8				; $E093 ......?.
			defb $7F,$FC,$FF,$FE,$F8,$3F,$F0,$7E				; $E09B .....?.~
			defb $E0,$FD,$C1,$FB,$83,$F7,$07,$EF				; $E0A3 ........
			defb $0F,$DF,$1F,$BF,$3F,$7E,$FE,$FD				; $E0AB ....?~..
			defb $FD,$FB,$FB,$F7,$F7,$EF,$0F,$CF				; $E0B3 ........
			defb $1F,$8F,$3F,$4F,$7E,$EF,$01,$FF				; $E0BB ..?O~...
			defb $FB,$FF,$F7,$FF,$EF,$DF,$DF,$8F				; $E0C3 ........
			defb $BF,$0F,$7E,$0F,$FC,$0F,$FF,$FF				; $E0CB ..~.....
			defb $FF,$FF,$FF,$FF,$FF,$FF,$80,$00				; $E0D3 ........
			defb $80,$00,$80,$00,$80,$00,$80,$00				; $E0DB ........
			defb $80,$00,$80,$00,$80,$00,$80,$00				; $E0E3 ........
			defb $80,$00,$80,$00,$80,$00,$80,$00				; $E0EB ........
			defb $80,$00,$80,$00,$80,$00,$FF,$EF				; $E0F3 ........
			defb $7F,$EF,$3F,$EF,$1F,$EF,$0F,$EF				; $E0FB ..?.....
			defb $07,$EF,$03,$EF,$01,$EF,$00,$0F				; $E103 ........
			defb $00,$7F,$00,$3F,$00,$1F,$00,$0F				; $E10B ...?....
			defb $00,$07,$00,$03,$00,$01,$80,$00				; $E113 ........
			defb $80,$00,$80,$00,$80,$00,$80,$00				; $E11B ........
			defb $83,$ED,$87,$DB,$83,$36,$86,$7D				; $E123 .....6.}
			defb $8C,$FB,$99,$B6,$B3,$6F,$A6,$DE				; $E12B .....o..
			defb $80,$00,$80,$00,$80,$00,$00,$00				; $E133 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E13B ........
			defb $BC,$0F,$78,$1E,$C0,$30,$E0,$7B				; $E143 ..x..0.{
			defb $C0,$F6,$01,$8D,$03,$1B,$06,$31				; $E14B .......1
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E153 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E15B ........
			defb $6E,$36,$DF,$6D,$36,$D8,$61,$F1				; $E163 n6.m6.a.
			defb $D3,$63,$B6,$C6,$ED,$8C,$DB,$19				; $E16B .c......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E173 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E17B ........
			defb $FB,$71,$F6,$FB,$C1,$B6,$9B,$6C				; $E183 .q.....l
			defb $36,$DA,$6D,$B6,$DB,$7C,$B6,$38				; $E18B 6.m..|.8
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E193 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E19B ........
			defb $C0,$31,$E0,$7F,$C0,$D6,$01,$8D				; $E1A3 .1......
			defb $03,$1B,$06,$36,$0C,$6D,$18,$DB				; $E1AB ...6.m..
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1B3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1BB ........
			defb $BC,$E3,$7D,$F6,$DB,$6D,$B6,$1F				; $E1C3 ..}..m..
			defb $EC,$3E,$D8,$6D,$BE,$DB,$1D,$B6				; $E1CB .>.m....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1D3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1DB ........
			defb $6D,$C7,$DB,$EF,$86,$D8,$6D,$BC				; $E1E3 m.....m.
			defb $DB,$78,$B6,$C0,$6D,$E0,$DB,$C0				; $E1EB .x..m...
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1F3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E1FB ........
			defb $80,$00,$00,$00,$00,$00,$00,$00				; $E203 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E20B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E213 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E21B ........
			defb $00,$00,$00,$00,$00,$00,$7F,$E0				; $E223 ........
			defb $3F,$C0,$1F,$80,$0F,$C0,$07,$E0				; $E22B ?.......
			defb $03,$F0,$01,$F8,$00,$1D,$00,$00				; $E233 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E23B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E243 ........
			defb $00,$00,$FF,$C0,$7F,$80,$3F,$00				; $E24B ......?.
			defb $7E,$00,$FC,$00,$F8,$00,$00,$00				; $E253 ~.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E25B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E263 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E26B ........
			defb $00,$00,$00,$0F,$00,$0F,$00,$00				; $E273 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E27B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E283 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E28B ........
			defb $00,$00,$E0,$00,$C0,$00,$00,$00				; $E293 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E29B ........
			defb $00,$00,$00,$00,$00,$50,$00,$22				; $E2A3 .....P."
			defb $03,$05,$07,$56,$BB,$76,$07,$56				; $E2AB ...V.v.V
			defb $03,$05,$00,$22,$00,$50,$03,$C0				; $E2B3 ...".P..
			defb $0F,$F0,$1C,$38,$3B,$1C,$37,$0C				; $E2BB ...8;.7.
			defb $36,$0C,$34,$04,$34,$08,$30,$04				; $E2C3 6.4.4.0.
			defb $34,$08,$30,$04,$30,$08,$38,$14				; $E2CB 4.0.0.8.
			defb $1C,$28,$0E,$D0,$03,$40,$03,$C0				; $E2D3 .(...@..
			defb $07,$E0,$0F,$F0,$0C,$30,$19,$18				; $E2DB .....0..
			defb $1B,$18,$1B,$18,$1A,$08,$18,$10				; $E2E3 ........
			defb $1A,$08,$18,$10,$18,$08,$0C,$30				; $E2EB .......0
			defb $0F,$50,$07,$A0,$03,$40,$01,$80				; $E2F3 .P...@..
			defb $03,$C0,$07,$E0,$0E,$70,$1C,$38				; $E2FB .....p.8
			defb $39,$1C,$73,$0E,$E7,$07,$E0,$05				; $E303 9.s.....
			defb $70,$0A,$38,$14,$1C,$28,$0E,$50				; $E30B p.8..(.P
			defb $06,$A0,$03,$40,$01,$80,$01,$80				; $E313 ...@....
			defb $03,$40,$07,$A0,$0F,$50,$1F,$A8				; $E31B .@...P..
			defb $3F,$54,$7F,$AA,$FF,$55,$AA,$01				; $E323 ?T...U..
			defb $55,$02,$2A,$04,$15,$08,$0A,$10				; $E32B U.*.....
			defb $05,$20,$02,$40,$01,$80,$0F,$D0				; $E333 . .@....
			defb $08,$10,$00,$00,$17,$E8,$2F,$F4				; $E33B ....../.
			defb $2F,$E8,$2F,$D4,$00,$00,$5F,$EA				; $E343 /./..._.
			defb $5F,$F4,$5F,$EA,$5F,$F4,$5F,$EA				; $E34B _._._._.
			defb $5F,$F4,$5F,$EA,$2F,$D4,$00,$00				; $E353 _._./...
			defb $81,$02,$6D,$6C,$4B,$A4,$1C,$70				; $E35B ..mlK..p
			defb $7F,$FC,$5E,$F4,$2D,$68,$ED,$6E				; $E363 ..^.-h.n
			defb $2D,$68,$5E,$F4,$7F,$FC,$1C,$70				; $E36B -h^....p
			defb $4B,$A4,$6D,$6C,$81,$02,$00,$00				; $E373 K.ml....
			defb $FF,$FF,$3F,$FF,$55,$55,$00,$00				; $E37B ..?.UU..
			defb $AA,$AA,$55,$55,$AA,$AA,$55,$55				; $E383 ..UU..UU
			defb $AA,$AA,$55,$55,$00,$00,$FF,$FF				; $E38B ..UU....
			defb $3F,$FF,$55,$55,$00,$00,$7F,$FE				; $E393 ?.UU....
			defb $7F,$FC,$60,$06,$6D,$B4,$69,$26				; $E39B ..`.m.i&
			defb $69,$24,$60,$06,$6F,$F4,$6C,$06				; $E3A3 i$`.o.l.
			defb $60,$04,$6D,$B6,$69,$24,$69,$26				; $E3AB `.m.i$i&
			defb $60,$04,$7F,$FE,$55,$54,$72,$AE				; $E3B3 `...UTr.
			defb $65,$4C,$72,$AE,$65,$4C,$72,$AE				; $E3BB eLr.eLr.
			defb $65,$4C,$72,$AE,$65,$4C,$72,$AE				; $E3C3 eLr.eLr.
			defb $65,$4C,$72,$AE,$65,$4C,$72,$AE				; $E3CB eLr.eLr.
			defb $65,$4C,$52,$AA,$45,$48,$00,$00				; $E3D3 eLR.EH..
			defb $7E,$F8,$9B,$F5,$C7,$FE,$70,$7D				; $E3DB ~.....p}
			defb $FF,$6E,$7F,$1D,$EF,$7E,$FE,$FD				; $E3E3 .n...~..
			defb $BF,$FE,$7F,$FD,$DD,$F4,$FF,$F9				; $E3EB ........
			defb $6F,$FE,$BF,$FD,$FD,$FE,$BF,$FD				; $E3F3 o.......
			defb $DD,$FA,$FB,$FD,$7F,$FA,$1F,$FC				; $E3FB ........
			defb $6E,$F8,$FF,$F5,$F9,$FA,$EF,$FD				; $E403 n.......
			defb $BF,$FA,$7B,$FC,$FF,$FA,$9F,$FD				; $E40B ..{.....
			defb $7F,$FA,$FB,$FD,$BF,$FA,$BF,$DD				; $E413 ........
			defb $DF,$FA,$FF,$F5,$77,$EA,$BF,$FC				; $E41B ....w...
			defb $EF,$FA,$F8,$FC,$FF,$BA,$AF,$F9				; $E423 ........
			defb $FF,$FA,$7E,$FD,$7F,$FA,$9F,$F5				; $E42B ..~.....
			defb $FF,$EA,$F7,$FD,$7F,$FA,$EF,$FD				; $E433 ........
			defb $7E,$F8,$9B,$F5,$C7,$FE,$70,$7D				; $E43B ~.....p}
			defb $FF,$6E,$7F,$1D,$EF,$7E,$FE,$FD				; $E443 .n...~..
			defb $BF,$FE,$7F,$FD,$DD,$F4,$FF,$F9				; $E44B ........
			defb $6F,$FE,$BF,$FD,$FD,$FE,$BF,$DD				; $E453 o.......
			defb $DF,$FA,$FF,$F5,$77,$EA,$BF,$FC				; $E45B ....w...
			defb $EF,$FA,$F8,$FC,$FF,$BA,$AF,$F9				; $E463 ........
			defb $FF,$FA,$7E,$FD,$7F,$FA,$9F,$F5				; $E46B ..~.....
			defb $FF,$EA,$F4,$0D,$00,$00,$00,$00				; $E473 ........
			defb $00,$00,$00,$01,$00,$03,$00,$06				; $E47B ........
			defb $00,$04,$00,$04,$00,$0C,$00,$39				; $E483 .......9
			defb $00,$33,$00,$F7,$03,$F6,$07,$9E				; $E48B .3......
			defb $3E,$7A,$7D,$E6,$E1,$C3,$00,$00				; $E493 >z}.....
			defb $A0,$02,$90,$12,$2A,$46,$60,$5B				; $E49B ....*F`[
			defb $72,$0D,$6E,$F5,$E6,$E6,$C6,$66				; $E4A3 r.n....f
			defb $0C,$63,$5D,$71,$18,$65,$30,$E3				; $E4AB .c]q.e0.
			defb $65,$CB,$D1,$A3,$C1,$83,$00,$00				; $E4B3 e.......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E4BB ........
			defb $00,$00,$80,$00,$C0,$00,$60,$00				; $E4C3 ......`.
			defb $30,$00,$BE,$00,$9A,$00,$9B,$60				; $E4CB 0......`
			defb $70,$FC,$6A,$C7,$C0,$D3,$00,$00				; $E4D3 p.j.....
			defb $FF,$FF,$7F,$F5,$00,$00,$0F,$C0				; $E4DB ........
			defb $00,$00,$07,$80,$03,$00,$C3,$00				; $E4E3 ........
			defb $C3,$00,$DD,$00,$CE,$00,$D7,$00				; $E4EB ........
			defb $DF,$00,$C0,$00,$C0,$00,$00,$00				; $E4F3 ........
			defb $FF,$FF,$55,$56,$00,$00,$03,$F0				; $E4FB ..UV....
			defb $00,$00,$01,$E0,$00,$C0,$00,$C3				; $E503 ........
			defb $00,$C2,$00,$BB,$00,$72,$00,$EB				; $E50B .....r..
			defb $00,$FA,$00,$03,$00,$02,$07,$00				; $E513 ........
			defb $1F,$78,$3F,$34,$7F,$7A,$7F,$34				; $E51B .x?4.z.4
			defb $FF,$7A,$FF,$34,$FE,$7A,$00,$34				; $E523 .z.4.z.4
			defb $45,$7A,$6F,$F4,$6F,$FA,$6F,$F4				; $E52B Ezo.o.o.
			defb $6F,$FA,$6F,$F4,$6F,$FA,$00,$E0				; $E533 o.o.o...
			defb $2E,$F8,$6E,$FC,$6E,$FE,$6E,$FE				; $E53B ..n.n.n.
			defb $6E,$FF,$6E,$FF,$6E,$7F,$6E,$00				; $E543 n.n.n.n.
			defb $6E,$AA,$6F,$D4,$6F,$EA,$6F,$F4				; $E54B n.o.o.o.
			defb $6F,$EA,$6F,$F4,$6F,$EA,$6F,$F4				; $E553 o.o.o.o.
			defb $6F,$EA,$6F,$F4,$6F,$EA,$6F,$F4				; $E55B o.o.o.o.
			defb $6F,$EA,$6F,$F4,$6F,$EA,$6F,$F4				; $E563 o.o.o.o.
			defb $6F,$EA,$6F,$F4,$6F,$EA,$6F,$F4				; $E56B o.o.o.o.
			defb $6F,$EA,$6F,$F4,$6F,$EA,$00,$00				; $E573 o.o.o...
			defb $FF,$FF,$80,$00,$95,$55,$AA,$AA				; $E57B .....U..
			defb $95,$55,$AA,$AA,$95,$55,$AA,$AA				; $E583 .U...U..
			defb $95,$55,$AA,$AA,$95,$55,$AA,$AA				; $E58B .U...U..
			defb $95,$55,$AA,$AA,$95,$55,$00,$00				; $E593 .U...U..
			defb $00,$00,$00,$00,$00,$00,$00,$1F				; $E59B ........
			defb $00,$00,$00,$3F,$00,$00,$00,$7F				; $E5A3 ...?....
			defb $00,$7F,$00,$70,$00,$78,$00,$7C				; $E5AB ...p.x.|
			defb $00,$7E,$00,$FF,$1F,$FD,$00,$00				; $E5B3 .~......
			defb $00,$00,$00,$00,$00,$00,$A8,$00				; $E5BB ........
			defb $00,$00,$D4,$00,$00,$00,$EA,$00				; $E5C3 ........
			defb $F4,$00,$0A,$00,$14,$00,$2A,$00				; $E5CB ......*.
			defb $74,$00,$EA,$00,$55,$F8,$00,$00				; $E5D3 t...U...
			defb $FF,$FD,$F3,$CA,$F3,$CD,$F3,$CA				; $E5DB ........
			defb $E1,$CD,$DE,$CA,$E1,$CD,$F3,$CA				; $E5E3 ........
			defb $F3,$85,$F3,$7A,$F3,$85,$F3,$CA				; $E5EB ...z....
			defb $F3,$C5,$FF,$EA,$00,$00,$0E,$C0				; $E5F3 ........
			defb $3E,$50,$7E,$AC,$FE,$55,$00,$00				; $E5FB >P~..U..
			defb $FF,$FF,$7F,$FC,$0F,$E0,$00,$00				; $E603 ........
			defb $77,$C0,$EF,$BA,$5F,$00,$00,$00				; $E60B w..._...
			defb $7B,$00,$FB,$6A,$7B,$00,$06,$E0				; $E613 {..j{...
			defb $1F,$F8,$3E,$F4,$7F,$FA,$00,$00				; $E61B ..>.....
			defb $FE,$FD,$FF,$FA,$FD,$F5,$F7,$FA				; $E623 ........
			defb $5F,$F5,$FF,$EA,$00,$00,$7F,$EA				; $E62B _.......
			defb $3F,$D4,$1F,$68,$06,$A0,$00,$00				; $E633 ?..h....
			defb $00,$00,$00,$00,$00,$00,$07,$E0				; $E63B ........
			defb $04,$20,$70,$0A,$75,$6C,$72,$AA				; $E643 . p.ulr.
			defb $70,$0E,$04,$20,$06,$A0,$00,$00				; $E64B p.. ....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $E653 ........
			defb $03,$C0,$01,$80,$00,$00,$07,$E0				; $E65B ........
			defb $04,$20,$70,$0A,$75,$6C,$72,$AA				; $E663 . p.ulr.
			defb $70,$0E,$04,$20,$06,$A0,$00,$00				; $E66B p.. ....
			defb $01,$80,$03,$C0,$00,$00,$00,$00				; $E673 ........
			defb $03,$40,$03,$80,$03,$40,$00,$00				; $E67B .@...@..
			defb $0D,$B0,$09,$00,$09,$90,$09,$00				; $E683 ........
			defb $09,$90,$0D,$30,$00,$00,$03,$40				; $E68B ...0...@
			defb $03,$80,$03,$40,$00,$00,$37,$F4				; $E693 ...@..7.
			defb $6F,$FA,$6F,$FA,$6F,$F4,$6F,$FA				; $E69B o.o.o.o.
			defb $6F,$F4,$40,$02,$37,$F4,$37,$E8				; $E6A3 o.@.7.7.
			defb $37,$F4,$37,$E8,$37,$F4,$37,$E8				; $E6AB 7.7.7.7.
			defb $37,$F4,$37,$E8,$37,$F4,$37,$E8				; $E6B3 7.7.7.7.
			defb $37,$F4,$37,$E8,$37,$F4,$37,$E8				; $E6BB 7.7.7.7.
			defb $37,$F4,$37,$E8,$37,$F4,$37,$E8				; $E6C3 7.7.7.7.
			defb $37,$D4,$37,$E8,$37,$D4,$37,$E8				; $E6CB 7.7.7.7.
			defb $37,$D4,$37,$A8,$37,$54,$00,$00				; $E6D3 7.7.7T..
			defb $DF,$FF,$DF,$F5,$00,$00,$77,$EA				; $E6DB ......w.
			defb $00,$00,$1B,$E8,$1B,$F0,$1B,$E8				; $E6E3 ........
			defb $1B,$F0,$1B,$E8,$1B,$F0,$1B,$E8				; $E6EB ........
			defb $00,$00,$77,$EA,$EF,$55,$4F,$F4				; $E6F3 ..w..UO.
			defb $F3,$F9,$3C,$F5,$CF,$39,$73,$CC				; $E6FB ..<..9s.
			defb $28,$F0,$35,$3C,$2A,$8F,$35,$57				; $E703 (.5<*.5W
			defb $2A,$AB,$35,$53,$6A,$AB,$D5,$53				; $E70B *.5Sj..S
			defb $00,$00,$FF,$FF,$3F,$FC,$00,$00				; $E713 ....?...
			defb $80,$00,$B0,$00,$B8,$00,$78,$00				; $E71B ......x.
			defb $20,$00,$0C,$00,$3C,$00,$5C,$00				; $E723  ...<.\.
			defb $60,$00,$78,$00,$70,$00,$40,$00				; $E72B `.x.p.@.
			defb $00,$00,$00,$00,$00,$00,$C0,$03				; $E733 ........
			defb $FF,$FF,$00,$00,$6F,$F6,$4B,$D4				; $E73B ....o.K.
			defb $6F,$F6,$4F,$F4,$6F,$F6,$4F,$F4				; $E743 o.O.o.O.
			defb $6F,$F6,$4F,$F4,$6F,$F6,$4F,$F4				; $E74B o.O.o.O.
			defb $6B,$D6,$4F,$F4,$6F,$F6,$00,$00				; $E753 k.O.o...
			defb $00,$01,$00,$0D,$00,$1D,$00,$1E				; $E75B ........
			defb $00,$04,$00,$30,$00,$3C,$00,$3A				; $E763 ...0.<.:
			defb $00,$06,$00,$1E,$00,$0E,$00,$02				; $E76B ........
			defb $00,$00,$00,$00,$00,$00,$2F,$F2				; $E773 ....../.
			defb $9F,$CF,$AF,$3C,$9C,$F3,$33,$CE				; $E77B ...<..3.
			defb $0F,$14,$3C,$AC,$F1,$54,$EA,$AC				; $E783 ..<..T..
			defb $D5,$54,$CA,$AC,$D5,$56,$CA,$AB				; $E78B .T...V..
			defb $00,$00,$FF,$FF,$3F,$FC,$6F,$F6				; $E793 ....?.o.
			defb $4F,$F4,$6B,$D6,$4F,$F4,$6F,$F6				; $E79B O.k.O.o.
			defb $4F,$F4,$6F,$F6,$4F,$F4,$6F,$F6				; $E7A3 O.o.O.o.
			defb $4F,$F4,$6F,$F6,$4B,$D4,$6F,$F6				; $E7AB O.o.K.o.
			defb $00,$00,$FF,$FF,$C0,$03,$00,$00				; $E7B3 ........
			defb $7F,$FF,$5F,$FF,$60,$00,$60,$03				; $E7BB .._.`.`.
			defb $60,$0C,$6C,$30,$63,$00,$60,$C0				; $E7C3 `.l0c.`.
			defb $6C,$30,$60,$0C,$60,$03,$60,$00				; $E7CB l0`.`.`.
			defb $5F,$FF,$55,$55,$00,$00,$00,$00				; $E7D3 _.UU....
			defb $FF,$FF,$FF,$FF,$00,$00,$C0,$03				; $E7DB ........
			defb $30,$0C,$0C,$30,$03,$00,$00,$C0				; $E7E3 0..0....
			defb $0C,$30,$30,$0C,$C0,$03,$00,$00				; $E7EB .00.....
			defb $FF,$FF,$55,$55,$00,$00,$00,$00				; $E7F3 ..UU....
			defb $FF,$FE,$FF,$FA,$00,$06,$C0,$06				; $E7FB ........
			defb $30,$06,$0C,$34,$00,$C6,$03,$04				; $E803 0..4....
			defb $0C,$32,$30,$04,$C0,$02,$00,$04				; $E80B .20.....
			defb $FF,$FA,$55,$54,$00,$00,$00,$00				; $E813 ..UT....
			defb $7F,$FE,$5F,$F8,$60,$06,$62,$44				; $E81B .._.`.bD
			defb $62,$46,$61,$04,$61,$06,$60,$84				; $E823 bFa.a.`.
			defb $60,$86,$62,$44,$62,$46,$64,$24				; $E82B `.bDbFd$
			defb $64,$26,$68,$14,$68,$16,$68,$14				; $E833 d&h.h.h.
			defb $68,$16,$64,$24,$64,$26,$62,$44				; $E83B h.d$d&bD
			defb $62,$46,$61,$04,$61,$06,$60,$84				; $E843 bFa.a.`.
			defb $60,$86,$62,$44,$62,$46,$64,$24				; $E84B `.bDbFd$
			defb $64,$26,$68,$14,$68,$16,$68,$14				; $E853 d&h.h.h.
			defb $68,$16,$64,$24,$64,$26,$62,$44				; $E85B h.d$d&bD
			defb $62,$46,$60,$84,$60,$86,$61,$04				; $E863 bF`.`.a.
			defb $61,$06,$62,$44,$62,$46,$60,$04				; $E86B a.bDbF`.
			defb $5F,$AA,$7D,$54,$00,$00,$00,$00				; $E873 _.}T....
			defb $00,$00,$00,$00,$00,$00,$00,$01				; $E87B ........
			defb $00,$01,$00,$01,$00,$00,$00,$00				; $E883 ........
			defb $00,$04,$00,$08,$00,$10,$00,$10				; $E88B ........
			defb $00,$20,$00,$20,$00,$20,$20,$00				; $E893 . . .  .
			defb $40,$00,$80,$00,$80,$00,$08,$00				; $E89B @.......
			defb $04,$00,$02,$00,$83,$00,$C1,$00				; $E8A3 ........
			defb $41,$00,$61,$00,$21,$00,$32,$00				; $E8AB A.a.!.2.
			defb $18,$00,$0C,$00,$34,$00,$00,$20				; $E8B3 ....4.. 
			defb $00,$13,$00,$1A,$00,$0C,$00,$36				; $E8BB .......6
			defb $00,$76,$00,$63,$00,$C3,$00,$C3				; $E8C3 .v.c....
			defb $00,$C6,$00,$AE,$00,$E4,$00,$D0				; $E8CB ........
			defb $00,$68,$00,$74,$00,$BD,$C6,$00				; $E8D3 .h.t....
			defb $06,$00,$02,$00,$03,$00,$02,$00				; $E8DB ........
			defb $03,$00,$02,$00,$07,$00,$06,$00				; $E8E3 ........
			defb $0D,$00,$0A,$00,$1E,$00,$34,$00				; $E8EB ......4.
			defb $7C,$00,$E8,$00,$F0,$00,$03,$BB				; $E8F3 |.......
			defb $0F,$D7,$1F,$EF,$1F,$EF,$3F,$9F				; $E8FB ......?.
			defb $3F,$3F,$3E,$7F,$1E,$FF,$1A,$FE				; $E903 ??>.....
			defb $0D,$7D,$0B,$BE,$07,$BD,$0F,$B2				; $E90B .}......
			defb $1F,$69,$7E,$16,$E8,$C0,$D0,$00				; $E913 .i~.....
			defb $F3,$00,$A5,$C0,$A9,$A0,$D7,$50				; $E91B .......P
			defb $8F,$A0,$57,$D0,$4F,$E8,$4F,$D0				; $E923 ..W.O.O.
			defb $AB,$E8,$A5,$D0,$52,$E8,$A9,$90				; $E92B ....R...
			defb $54,$60,$AA,$90,$41,$5A,$00,$00				; $E933 T`..AZ..
			defb $0D,$DF,$0D,$FF,$0D,$DF,$0D,$FF				; $E93B ........
			defb $0D,$DF,$0D,$FF,$0D,$DF,$00,$00				; $E943 ........
			defb $3B,$7F,$77,$FF,$76,$FF,$EF,$FF				; $E94B ;.w.v...
			defb $EE,$FF,$EF,$FF,$00,$00,$00,$00				; $E953 ........
			defb $D5,$50,$EA,$A0,$D5,$50,$EA,$A0				; $E95B .P...P..
			defb $D5,$50,$AA,$A0,$55,$50,$00,$00				; $E963 .P..UP..
			defb $EA,$A8,$F5,$56,$FA,$AA,$F5,$55				; $E96B ...V...U
			defb $FA,$AA,$F5,$55,$00,$00,$01,$80				; $E973 ...U....
			defb $FD,$3F,$FD,$BF,$55,$15,$01,$80				; $E97B .?..U...
			defb $07,$E0,$04,$20,$F5,$6F,$54,$A5				; $E983 ... .oT.
			defb $05,$60,$07,$E0,$01,$80,$FD,$3F				; $E98B .`.....?
			defb $A9,$AA,$55,$35,$01,$80,$61,$0C				; $E993 ..U5..a.
			defb $71,$8A,$61,$0C,$71,$8A,$60,$0C				; $E99B q.a.q.`.
			defb $77,$EA,$04,$20,$FD,$7F,$AC,$B5				; $E9A3 w.. ....
			defb $05,$60,$67,$EE,$70,$0A,$61,$0C				; $E9AB .`g.p.a.
			defb $71,$8A,$61,$0C,$71,$8A,$00,$00				; $E9B3 q.a.q...
			defb $00,$00,$43,$43,$42,$43,$43,$42				; $E9BB ..CCBCCB
			defb $42,$43,$43,$42,$42,$43,$43,$42				; $E9C3 BCCBBCCB
			defb $43,$43,$43,$42,$42,$43,$00,$44				; $E9CB CCCBBC.D
			defb $44,$44,$44,$04,$44,$04,$04,$00				; $E9D3 DDD.D...
			defb $04,$04,$47,$45,$47,$45,$44,$04				; $E9DB ..GEGED.
			defb $44,$04,$47,$45,$47,$45,$42,$43				; $E9E3 D.GEGEBC
			defb $43,$42,$00,$00,$43,$42,$42,$43				; $E9EB CB..CBBC
			defb $43,$42,$00,$00,$43,$42,$47,$45				; $E9F3 CB..CBGE
			defb $47,$45,$47,$45,$47,$45,$47,$45				; $E9FB GEGEGEGE
			defb $47,$45,$47,$45,$47,$45,$46,$46				; $EA03 GEGEGEFF
			defb $06,$06,$46,$06,$46,$06,$47,$45				; $EA0B ..F.F.GE
			defb $47,$05,$45,$07,$05,$07,$47,$46				; $EA13 G.E...GF
			defb $06,$06,$43,$43,$42,$42,$43,$43				; $EA1B ..CCBBCC
			defb $42,$42,$43,$43,$42,$42,$43,$43				; $EA23 BBCCBBCC
			defb $42,$42,$47,$07,$47,$07,$47,$07				; $EA2B BBG.G.G.
			defb $47,$07,$47,$07,$47,$07,$47,$07				; $EA33 G.G.G.G.
			defb $47,$07,$47,$07,$47,$07,$47,$07				; $EA3B G.G.G.G.
			defb $47,$07,$44,$44,$44,$44,$04,$04				; $EA43 G.DDDD..
			defb $04,$04,$44,$44,$44,$44,$04,$04				; $EA4B ..DDDD..
			defb $04,$04,$47,$46,$46,$46,$06,$00				; $EA53 ..GFFF..
			defb $06,$00,$06,$06,$47,$46,$06,$00				; $EA5B ....GF..
			defb $06,$06,$47,$46,$45,$05,$46,$06				; $EA63 ..GFE.F.
			defb $46,$06,$46,$47,$00,$00,$05,$44				; $EA6B F.FG...D
			defb $44,$04,$05,$04,$44,$04,$47,$06				; $EA73 D...D.G.
			defb $06,$06,$06,$06,$06,$00,$46,$46				; $EA7B ......FF
			defb $47,$46,$06,$00,$06,$00,$43,$42				; $EA83 GF....CB
			defb $42,$43,$43,$42,$00,$00,$43,$42				; $EA8B BCCB..CB
			defb $42,$43,$43,$42,$00,$00,$47,$47				; $EA93 BCCB..GG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $EA9B GGGGGGGG
			defb $47,$47,$45,$04,$45,$04,$45,$04				; $EAA3 GGE.E.E.
			defb $45,$04,$45,$04,$45,$04,$47,$47				; $EAAB E.E.E.GG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $EAB3 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $EABB GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$47				; $EAC3 GGGGGGGG
			defb $47,$47,$47,$47,$47,$47,$47,$45				; $EACB GGGGGGGE
			defb $47,$45,$47,$45,$47,$45,$47,$45				; $EAD3 GEGEGEGE
			defb $47,$05,$47,$45,$47,$05,$47,$45				; $EADB G.GEG.GE
			defb $47,$05,$47,$45,$47,$05,$47,$46				; $EAE3 G.GEG.GF
			defb $47,$46,$06,$06,$06,$06,$47,$45				; $EAEB GF....GE
			defb $45,$44,$44,$04,$04,$04,$45,$44				; $EAF3 EDD...ED
			defb $47,$05,$04,$04,$44,$04,$47,$07				; $EAFB G...D.G.
			defb $47,$07,$07,$07,$07,$07,$47,$07				; $EB03 G.....G.
			defb $47,$07,$07,$07,$07,$07,$47,$07				; $EB0B G.....G.
			defb $47,$07,$07,$07,$07,$07,$47,$07				; $EB13 G.....G.
			defb $47,$07,$07,$07,$07,$07,$47,$47				; $EB1B G.....GG
			defb $00,$46,$46,$46,$06,$06,$46,$46				; $EB23 .FFF..FF
			defb $06,$06,$46,$46,$06,$06,$46,$46				; $EB2B ..FF..FF
			defb $06,$06,$46,$46,$06,$06,$46,$46				; $EB33 ..FF..FF
			defb $06,$06,$46,$46,$06,$06,$46,$46				; $EB3B ..FF..FF
			defb $06,$06,$46,$00,$06,$00,$06,$06				; $EB43 ..F.....
			defb $00,$06,$06,$46,$06,$06,$46,$46				; $EB4B ...F..FF
			defb $06,$06,$46,$46,$06,$06,$46,$46				; $EB53 ..FF..FF
			defb $06,$06,$46,$46,$06,$06,$46,$46				; $EB5B ..FF..FF
			defb $06,$06,$46,$46,$06,$06,$46,$00				; $EB63 ..FF..F.
			defb $00,$00,$00,$00,$47,$47,$00,$00				; $EB6B ....GG..
			defb $47,$47,$00,$00,$00,$47,$00,$00				; $EB73 GG...G..
			defb $47,$00,$00,$47,$47,$47,$45,$04				; $EB7B G..GGGE.
			defb $45,$04,$43,$42,$43,$42,$47,$46				; $EB83 E.CBCBGF
			defb $47,$46,$45,$05,$45,$05,$47,$46				; $EB8B GFE.E.GF
			defb $47,$46,$47,$07,$47,$07,$44,$44				; $EB93 GFG.G.DD
			defb $04,$04,$45,$44,$04,$04,$44,$04				; $EB9B ..ED..D.
			defb $44,$04,$47,$45,$47,$45,$47,$45				; $EBA3 D.GEGEGE
			defb $47,$45,$47,$45,$47,$45,$47,$45				; $EBAB GEGEGEGE
			defb $47,$45,$47,$45,$47,$45,$00,$42				; $EBB3 GEGEGE.B
			defb $42,$43,$43,$42,$42,$43,$43,$00				; $EBBB BCCBBCC.
			defb $42,$43,$47,$47,$47,$00,$47,$07				; $EBC3 BCGGG.G.
			defb $00,$07,$46,$04,$44,$04,$44,$06				; $EBCB ..F.D.D.
			defb $44,$04,$44,$04,$44,$04,$44,$04				; $EBD3 D.D.D.D.
			defb $44,$04,$00,$47,$47,$47,$47,$00				; $EBDB D..GGGG.
			defb $47,$07,$47,$45,$47,$45,$47,$47				; $EBE3 G.GEGEGG
			defb $47,$47,$47,$07,$07,$07,$47,$07				; $EBEB GGG...G.
			defb $07,$07,$47,$07,$07,$07,$47,$07				; $EBF3 ..G...G.
			defb $47,$07,$44,$04,$44,$04,$44,$04				; $EBFB G.D.D.D.
			defb $44,$04,$44,$04,$44,$04,$47,$07				; $EC03 D.D.D.G.
			defb $47,$07,$07,$00,$07,$00,$47,$46				; $EC0B G.....GF
			defb $46,$06,$00,$47,$00,$47,$47,$07				; $EC13 F..G.GG.
			defb $47,$07,$46,$06,$46,$06,$44,$44				; $EC1B G.F.F.DD
			defb $04,$04,$44,$44,$04,$04,$44,$44				; $EC23 ..DD..DD
			defb $04,$04,$44,$04,$44,$04,$44,$04				; $EC2B ..D.D.D.
			defb $44,$04,$44,$04,$44,$04,$00,$47				; $EC33 D.D.D..G
			defb $00,$47,$45,$00,$45,$00,$00,$45				; $EC3B .GE.E..E
			defb $00,$44,$44,$00,$44,$00,$45,$44				; $EC43 .DD.D.ED
			defb $45,$44,$04,$04,$04,$04,$45,$44				; $EC4B ED....ED
			defb $47,$45,$04,$04,$44,$04,$45,$45				; $EC53 GE..D.EE
			defb $44,$44,$45,$44,$45,$44,$C0,$00				; $EC5B DDEDED..
			defb $E0,$00,$14,$00,$77,$00,$14,$00				; $EC63 ....w...
			defb $E0,$00,$C0,$00,$00,$00,$30,$00				; $EC6B ......0.
			defb $38,$00,$05,$00,$1D,$C0,$05,$00				; $EC73 8.......
			defb $38,$00,$30,$00,$00,$00,$0C,$00				; $EC7B 8.0.....
			defb $0E,$00,$01,$40,$07,$70,$01,$40				; $EC83 ...@.p.@
			defb $0E,$00,$0C,$00,$00,$00,$03,$00				; $EC8B ........
			defb $03,$80,$00,$50,$01,$DC,$00,$50				; $EC93 ...P...P
			defb $03,$80,$03,$00,$00,$00,$00,$00				; $EC9B ........
			defb $03,$00,$07,$00,$28,$00,$EE,$00				; $ECA3 ....(...
			defb $28,$00,$07,$00,$03,$00,$00,$00				; $ECAB (.......
			defb $00,$C0,$01,$C0,$0A,$00,$3B,$80				; $ECB3 ......;.
			defb $0A,$00,$01,$C0,$00,$C0,$00,$00				; $ECBB ........
			defb $00,$30,$00,$70,$02,$80,$0E,$E0				; $ECC3 .0.p....
			defb $02,$80,$00,$70,$00,$30,$00,$00				; $ECCB ...p.0..
			defb $00,$0C,$00,$1C,$00,$A0,$03,$B8				; $ECD3 ........
			defb $00,$A0,$00,$1C,$00,$0C,$1D,$10				; $ECDB ........
			defb $7B,$84,$7E,$A1,$FD,$48,$FE,$81				; $ECE3 {.~..H..
			defb $FD,$50,$3A,$82,$35,$08,$07,$40				; $ECEB .P:.5..@
			defb $5E,$E2,$1F,$A0,$BF,$54,$3F,$A0				; $ECF3 ^....T?.
			defb $3F,$52,$0E,$A4,$2D,$40,$01,$D4				; $ECFB ?R..-@..
			defb $27,$B8,$07,$E9,$4F,$D4,$0F,$E8				; $ED03 '...O...
			defb $2F,$D5,$03,$A8,$03,$50,$00,$74				; $ED0B /....P.t
			defb $45,$EE,$11,$FA,$03,$F5,$AB,$FA				; $ED13 E.......
			defb $03,$F5,$08,$EA,$12,$D4,$08,$00				; $ED1B ........
			defb $49,$00,$1C,$00,$FF,$80,$1C,$00				; $ED23 I.......
			defb $1C,$00,$49,$00,$08,$00,$02,$00				; $ED2B ..I.....
			defb $12,$40,$17,$40,$2D,$A0,$07,$00				; $ED33 .@.@-...
			defb $07,$80,$12,$40,$02,$00,$00,$80				; $ED3B ...@....
			defb $06,$90,$01,$C0,$0E,$D8,$01,$80				; $ED43 ........
			defb $03,$C8,$04,$90,$00,$80,$00,$20				; $ED4B ....... 
			defb $01,$28,$00,$70,$03,$EE,$00,$30				; $ED53 .(.p...0
			defb $01,$74,$00,$20,$00,$20,$18,$00				; $ED5B .t. . ..
			defb $42,$00,$18,$00,$BD,$00,$BD,$00				; $ED63 B.......
			defb $18,$00,$42,$00,$18,$00,$06,$00				; $ED6B ..B.....
			defb $10,$80,$06,$00,$2F,$40,$2F,$40				; $ED73 ..../@/@
			defb $06,$00,$10,$80,$06,$00,$01,$80				; $ED7B ........
			defb $04,$20,$01,$80,$0B,$D0,$0B,$D0				; $ED83 . ......
			defb $01,$80,$04,$20,$01,$80,$00,$60				; $ED8B ... ...`
			defb $01,$08,$00,$60,$02,$F4,$02,$F4				; $ED93 ...`....
			defb $00,$60,$01,$08,$00,$60,$3C,$00				; $ED9B .`...`<.
			defb $4E,$00,$BF,$00,$BF,$00,$FF,$00				; $EDA3 N.......
			defb $FF,$00,$7E,$00,$3C,$00,$0F,$00				; $EDAB ..~.<...
			defb $13,$80,$2F,$C0,$2F,$C0,$3F,$C0				; $EDB3 .././.?.
			defb $3F,$C0,$1F,$80,$0F,$00,$00,$00				; $EDBB ?.......
			defb $03,$C0,$06,$E0,$05,$E0,$07,$E0				; $EDC3 ........
			defb $07,$E0,$03,$C0,$00,$00,$00,$00				; $EDCB ........
			defb $00,$F0,$01,$B8,$01,$78,$01,$F8				; $EDD3 .....x..
			defb $01,$F8,$00,$F0,$00,$00,$14,$00				; $EDDB ........
			defb $40,$00,$15,$00,$88,$00,$54,$00				; $EDE3 @.....T.
			defb $81,$00,$24,$00,$08,$00,$05,$00				; $EDEB ..$.....
			defb $10,$00,$05,$40,$22,$00,$15,$00				; $EDF3 ...@"...
			defb $20,$40,$09,$00,$02,$00,$01,$40				; $EDFB  @.....@
			defb $04,$00,$01,$50,$08,$80,$05,$40				; $EE03 ...P...@
			defb $08,$10,$02,$40,$00,$80,$00,$50				; $EE0B ...@...P
			defb $01,$00,$00,$54,$02,$20,$01,$50				; $EE13 ...T. .P
			defb $02,$04,$00,$90,$00,$20,$10,$00				; $EE1B ..... ..
			defb $04,$00,$28,$00,$52,$00,$08,$00				; $EE23 ..(.R...
			defb $50,$00,$04,$00,$00,$00,$04,$00				; $EE2B P.......
			defb $01,$00,$0A,$00,$14,$80,$02,$00				; $EE33 ........
			defb $14,$00,$01,$00,$00,$00,$01,$00				; $EE3B ........
			defb $00,$40,$02,$80,$05,$20,$00,$80				; $EE43 .@... ..
			defb $05,$00,$00,$40,$00,$00,$00,$40				; $EE4B ...@...@
			defb $00,$10,$00,$A0,$01,$48,$00,$20				; $EE53 .....H. 
			defb $01,$40,$00,$10,$00,$00,$00,$00				; $EE5B .@......
			defb $08,$00,$00,$00,$2A,$00,$10,$00				; $EE63 ....*...
			defb $08,$00,$00,$00,$00,$00,$00,$00				; $EE6B ........
			defb $02,$00,$00,$00,$0A,$80,$04,$00				; $EE73 ........
			defb $02,$00,$00,$00,$00,$00,$00,$00				; $EE7B ........
			defb $00,$80,$00,$00,$02,$A0,$01,$00				; $EE83 ........
			defb $00,$80,$00,$00,$00,$00,$00,$00				; $EE8B ........
			defb $00,$20,$00,$00,$00,$A8,$00,$40				; $EE93 . .....@
			defb $00,$20,$00,$00,$00,$00,$00,$00				; $EE9B . ......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EEA3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EEAB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EEB3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EEBB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EEC3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EECB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EED3 ........
			defb $00,$00,$00,$00,$00,$00,$3E,$00				; $EEDB ......>.
			defb $41,$00,$9C,$00,$A2,$00,$AA,$00				; $EEE3 A.......
			defb $92,$00,$42,$00,$3C,$00,$0F,$00				; $EEEB ..B.<...
			defb $10,$80,$26,$40,$29,$40,$25,$40				; $EEF3 ..&@)@%@
			defb $21,$40,$1E,$40,$00,$80,$03,$C0				; $EEFB !@.@....
			defb $04,$20,$04,$90,$05,$50,$04,$50				; $EF03 . ...P.P
			defb $03,$90,$08,$20,$07,$C0,$01,$00				; $EF0B ... ....
			defb $02,$78,$02,$84,$02,$A4,$02,$94				; $EF13 .x......
			defb $02,$64,$01,$08,$00,$F0,$00,$00				; $EF1B .d......
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EF23 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $EF2B ........
			defb $00,$00,$00,$00,$00                            ; $EF33 .....

			JP $EFE5				; $EF38

			JP $EF93				; $EF3B

			LD E,$01				; $EF3E
			LD A,$01				; $EF40
			LD C,A				; $EF42
			CALL $EFC1				; $EF43
			LD A,(HL)				; $EF46
			CP $09				; $EF47
			JP NC,$EF4E				; $EF49
			LD C,A				; $EF4C
			INC HL				; $EF4D
			LD A,C				; $EF4E
			LD IX,$F214				; $EF4F
			DEC A				; $EF53
			JP Z,$EF63				; $EF54
			LD IX,$F237				; $EF57
			DEC A				; $EF5B
			JP Z,$EF63				; $EF5C
			LD IX,$F25A				; $EF5F
			LD A,(HL)				; $EF63
			CP $F4				; $EF64
			LD A,$0A				; $EF66
			JP NZ,$EF6E				; $EF68
			INC HL				; $EF6B
			LD A,(HL)				; $EF6C
			INC HL				; $EF6D
			CP (IX+$10)				; $EF6E
			RET C				; $EF71
			LD (IX+$10),A				; $EF72
			LD (IX+$12),L				; $EF75
			LD (IX+$13),H				; $EF78
			LD (IX+$14),L				; $EF7B
			LD (IX+$15),H				; $EF7E
			LD (IX+$16),L				; $EF81
			LD (IX+$17),H				; $EF84
			LD (IX+$11),$01				; $EF87
			XOR A				; $EF8B
			LD (IX+$18),A				; $EF8C
			LD (IX+$20),A				; $EF8F
			RET				; $EF92

			XOR A				; $EF93
			LD ($F224),A				; $EF94
			LD ($F247),A				; $EF97
			LD ($F26A),A				; $EF9A
			LD ($F1FE),A				; $EF9D
			LD ($F1FF),A				; $EFA0
			LD ($F200),A				; $EFA3
			LD A,$3F				; $EFA6
			LD ($F1FD),A				; $EFA8
			LD HL,$F203				; $EFAB
			LD E,$0D				; $EFAE
			LD BC,$FFFD				; $EFB0
			OUT (C),E				; $EFB3
			LD BC,$BFFD				; $EFB5
			LD A,(HL)				; $EFB8
			DEC HL				; $EFB9
			OUT (C),A				; $EFBA
			DEC E				; $EFBC
			JP P,$EFB0				; $EFBD
			RET				; $EFC0

			LD A,E				; $EFC1
			ADD A,A				; $EFC2
			ADD A,$A6				; $EFC3
			LD L,A				; $EFC5
			ADC A,$F3				; $EFC6
			SUB L				; $EFC8
			LD H,A				; $EFC9
			LD E,(HL)				; $EFCA
			INC HL				; $EFCB
			LD D,(HL)				; $EFCC
			LD HL,$F28E				; $EFCD
			ADD HL,DE				; $EFD0
			RET				; $EFD1

			LD A,E				; $EFD2
			ADD A,A				; $EFD3
			ADD A,$56				; $EFD4
			LD E,A				; $EFD6
			ADC A,$F3				; $EFD7
			SUB E				; $EFD9
			LD D,A				; $EFDA
			LD A,(DE)				; $EFDB
			ADD A,$8E				; $EFDC
			LD C,A				; $EFDE
			INC DE				; $EFDF
			LD A,(DE)				; $EFE0
			ADC A,$F2				; $EFE1
			LD B,A				; $EFE3
			RET				; $EFE4

			CALL $EFAB				; $EFE5
			LD IX,$F214				; $EFE8
			LD HL,($F1F6)				; $EFEC
			CALL $F01B				; $EFEF
			LD HL,($F204)				; $EFF2
			LD ($F1F6),HL				; $EFF5
			LD HL,($F1F8)				; $EFF8
			LD IX,$F237				; $EFFB
			CALL $F01B				; $EFFF
			LD HL,($F204)				; $F002
			LD ($F1F8),HL				; $F005
			LD HL,($F1FA)				; $F008
			LD IX,$F25A				; $F00B
			CALL $F01B				; $F00F
			LD HL,($F204)				; $F012
			LD ($F1FA),HL				; $F015
			JP $F1B1				; $F018

			LD ($F204),HL				; $F01B
			LD A,(IX+$10)				; $F01E
			OR A				; $F021
			RET Z				; $F022
			DEC (IX+$11)				; $F023
			JP NZ,$F162				; $F026
			LD (IX+$1E),$14				; $F029
			LD H,(IX+$13)				; $F02D
			LD L,(IX+$12)				; $F030
			LD A,(HL)				; $F033
			INC HL				; $F034
			LD E,(HL)				; $F035
			INC HL				; $F036
			LD (IX+$13),H				; $F037
			LD (IX+$12),L				; $F03A
			DEC (IX+$1E)				; $F03D
			RET Z				; $F040
			CP $00				; $F041
			JP Z,$F116				; $F043
			CP $09				; $F046
			JP C,$F089				; $F048
			CP $65				; $F04B
			JP C,$F113				; $F04D
			CP $E4				; $F050
			JP Z,$F093				; $F052
			CP $E3				; $F055
			JP Z,$F111				; $F057
			CP $E1				; $F05A
			JP Z,$F0E6				; $F05C
			CP $E9				; $F05F
			JP Z,$F0A4				; $F061
			CP $E8				; $F064
			JP Z,$F0B0				; $F066
			CP $EA				; $F069
			JP Z,$F0BC				; $F06B
			CP $E2				; $F06E
			JP Z,$F0F9				; $F070
			CP $E5				; $F073
			JP Z,$F0C6				; $F075
			CP $E6				; $F078
			JP Z,$F09E				; $F07A
			CP $F0				; $F07D
			JP Z,$F0F3				; $F07F
			CP $FF				; $F082
			JP Z,$F0D2				; $F084
			JR $F033				; $F087

			PUSH IX				; $F089
			CALL $EF42				; $F08B
			POP IX				; $F08E
			JP $F02D				; $F090

			LD A,E				; $F093
			LD ($F1FC),A				; $F094
			LD (IX+$19),$01				; $F097
			JP $F033				; $F09B

			LD (IX+$18),E				; $F09E
			JP $F033				; $F0A1

			CALL $EFD2				; $F0A4
			LD (IX+$0E),C				; $F0A7
			LD (IX+$0F),B				; $F0AA
			JP $F033				; $F0AD

			CALL $EFD2				; $F0B0
			LD (IX+$0C),C				; $F0B3
			LD (IX+$0D),B				; $F0B6
			JP $F033				; $F0B9

			CALL $EFD2				; $F0BC
			LD ($F212),BC				; $F0BF
			JP $F033				; $F0C3

			LD (IX+$15),H				; $F0C6
			LD (IX+$14),L				; $F0C9
			CALL $EFC1				; $F0CC
			JP $F033				; $F0CF

			LD H,(IX+$15)				; $F0D2
			LD L,(IX+$14)				; $F0D5
			LD A,(HL)				; $F0D8
			INC A				; $F0D9
			JP NZ,$F033				; $F0DA
			LD H,(IX+$17)				; $F0DD
			LD L,(IX+$16)				; $F0E0
			JP $F033				; $F0E3

			LD (IX+$10),$00				; $F0E6
			LD H,(IX+$1D)				; $F0EA
			LD L,(IX+$1C)				; $F0ED
			LD (HL),$00				; $F0F0
			RET				; $F0F2

			LD (IX+$20),E				; $F0F3
			JP $F033				; $F0F6

			LD HL,$28B2				; $F0F9
			LD C,L				; $F0FC
			LD B,H				; $F0FD
			ADD HL,HL				; $F0FE
			ADD HL,HL				; $F0FF
			ADD HL,BC				; $F100
			ADD HL,HL				; $F101
			ADD HL,HL				; $F102
			ADD HL,HL				; $F103
			ADD HL,BC				; $F104
			LD ($F0FA),HL				; $F105
			LD A,H				; $F108
			AND E				; $F109
			INC A				; $F10A
			LD ($F112),A				; $F10B
			JP $F02D				; $F10E

			LD A,$2A				; $F111
			ADD A,(IX+$18)				; $F113
			LD (IX+$11),E				; $F116
			LD (IX+$21),A				; $F119
			CALL $F1A1				; $F11C
			LD H,(IX+$1D)				; $F11F
			LD L,(IX+$1C)				; $F122
			LD (HL),$00				; $F125
			PUSH IX				; $F127
			POP DE				; $F129
			LD HL,$0008				; $F12A
			ADD HL,DE				; $F12D
			LDI				; $F12E
			LDI				; $F130
			LDI				; $F132
			LDI				; $F134
			LDI				; $F136
			LDI				; $F138
			LDI				; $F13A
			LDI				; $F13C
			DEC (IX+$19)				; $F13E
			LD (IX+$19),$00				; $F141
			LD L,(IX+$1A)				; $F145
			JR NZ,$F156				; $F148
			LD HL,($F212)				; $F14A
			LD ($F20A),HL				; $F14D
			LD HL,$0000				; $F150
			LD ($F206),HL				; $F153
			LD A,($F1FD)				; $F156
			AND (IX+$1B)				; $F159
			OR L				; $F15C
			AND $3F				; $F15D
			LD ($F1FD),A				; $F15F
			CALL $F1C9				; $F162
			LD H,(IX+$1D)				; $F165
			LD L,(IX+$1C)				; $F168
			LD A,(HL)				; $F16B
			ADD A,C				; $F16C
			SUB $80				; $F16D
			LD (HL),A				; $F16F
			LD HL,($F204)				; $F170
			LD A,H				; $F173
			OR L				; $F174
			RET Z				; $F175
			LD A,(IX+$20)				; $F176
			OR A				; $F179
			JP NZ,$F192				; $F17A
			INC IX				; $F17D
			INC IX				; $F17F
			CALL $F1C9				; $F181
			LD HL,($F204)				; $F184
			LD B,$00				; $F187
			ADD HL,BC				; $F189
			LD C,$80				; $F18A
			SBC HL,BC				; $F18C
			LD ($F204),HL				; $F18E
			RET				; $F191

			DEC (IX+$22)				; $F192
			LD A,(IX+$21)				; $F195
			JR Z,$F1A1				; $F198
			ADD A,(IX+$20)				; $F19A
			LD (IX+$22),$01				; $F19D
			ADD A,A				; $F1A1
			ADD A,$8E				; $F1A2
			LD L,A				; $F1A4
			ADC A,$F2				; $F1A5
			SUB L				; $F1A7
			LD H,A				; $F1A8
			LD DE,$F204				; $F1A9
			LDI				; $F1AC
			LDI				; $F1AE
			RET				; $F1B0

			LD IX,$F206				; $F1B1
			CALL $F1C9				; $F1B5
			LD HL,$F1FC				; $F1B8
			LD A,(HL)				; $F1BB
			ADD A,C				; $F1BC
			SUB $80				; $F1BD
			LD (HL),A				; $F1BF
			CP $11				; $F1C0
			RET C				; $F1C2
			INC HL				; $F1C3
			LD A,(HL)				; $F1C4
			OR $38				; $F1C5
			LD (HL),A				; $F1C7
			RET				; $F1C8

			PUSH IX				; $F1C9
			POP HL				; $F1CB
			LD D,(IX+$05)				; $F1CC
			LD E,(IX+$04)				; $F1CF
			INC (HL)				; $F1D2
			LD A,(DE)				; $F1D3
			SUB (HL)				; $F1D4
			LD C,$80				; $F1D5
			RET NZ				; $F1D7
			LD (HL),A				; $F1D8
			INC DE				; $F1D9
			LD A,(DE)				; $F1DA
			LD C,A				; $F1DB
			INC DE				; $F1DC
			INC HL				; $F1DD
			INC (HL)				; $F1DE
			LD A,(DE)				; $F1DF
			SUB (HL)				; $F1E0
			RET NZ				; $F1E1
			LD (HL),A				; $F1E2
			INC DE				; $F1E3
			LD A,(DE)				; $F1E4
			INC A				; $F1E5
			JP NZ,$F1EF				; $F1E6
			LD D,(IX+$0D)				; $F1E9
			LD E,(IX+$0C)				; $F1EC
			LD (IX+$05),D				; $F1EF
			LD (IX+$04),E				; $F1F2
			RET				; $F1F5

			defb $A8,$01,$00,$00,$00                            ; $F1F6 .....
			defb $00,$2F,$3F,$00,$00,$00,$64,$00				; $F1FB ./?...d.
			defb $0A,$00,$00,$4A,$01,$0A,$00,$00				; $F203 ...J....
			defb $00,$04,$00,$00,$00,$12,$00,$00				; $F20B ........
			defb $00,$00,$02,$00,$00,$1B,$F4,$71				; $F213 .......q
			defb $F4,$00,$00,$00,$00,$18,$F4,$71				; $F21B .......q
			defb $F4,$00,$0A,$5E,$F6,$75,$F5,$6F				; $F223 ...^.u.o
			defb $F5,$07,$00,$08,$36,$FE,$F1,$12				; $F22B ....6...
			defb $00,$10,$29,$01,$00,$00,$00,$00				; $F233 ..).....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F23B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F243 ........
			defb $00,$00,$00,$00,$00,$00,$10,$2D				; $F24B .......-
			defb $FF,$F1,$00,$00,$00,$00,$00,$00				; $F253 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F25B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F263 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F26B ........
			defb $00,$20,$1B,$00,$F2,$00,$00,$00				; $F273 . ......
			defb $00,$00,$2A,$00,$00,$00,$00,$00				; $F27B ..*.....
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $F283 ........
			defb $00,$00,$00,$00,$00,$17,$2A,$BA				; $F28B ......*.
			defb $27,$80,$25,$65,$23,$68,$21,$88				; $F293 '.%e#h!.
			defb $1F,$C3,$1D,$18,$1C,$84,$1A,$07				; $F29B ........
			defb $19,$9F,$17,$4C,$16,$0C,$15,$DD				; $F2A3 ...L....
			defb $13,$C0,$12,$B2,$11,$B4,$10,$C4				; $F2AB ........
			defb $0F,$E2,$0E,$0C,$0E,$42,$0D,$84				; $F2B3 .....B..
			defb $0C,$D0,$0B,$26,$0B,$86,$0A,$EF				; $F2BB ...&....
			defb $09,$60,$09,$D9,$08,$5A,$08,$E2				; $F2C3 .`...Z..
			defb $07,$71,$07,$06,$07,$A1,$06,$42				; $F2CB .q.....B
			defb $06,$E8,$05,$93,$05,$43,$05,$F7				; $F2D3 .....C..
			defb $04,$B0,$04,$6D,$04,$2D,$04,$F1				; $F2DB ...m.-..
			defb $03,$B8,$03,$83,$03,$50,$03,$21				; $F2E3 .....P.!
			defb $03,$F4,$02,$CA,$02,$A1,$02,$7C				; $F2EB .......|
			defb $02,$58,$02,$36,$02,$17,$02,$F9				; $F2F3 .X.6....
			defb $01,$DC,$01,$C1,$01,$A8,$01,$90				; $F2FB ........
			defb $01,$7A,$01,$65,$01,$51,$01,$3E				; $F303 .z.e.Q.>
			defb $01,$2C,$01,$1B,$01,$0B,$01,$FC				; $F30B .,......
			defb $00,$EE,$00,$E1,$00,$D4,$00,$C8				; $F313 ........
			defb $00,$BD,$00,$B2,$00,$A8,$00,$9F				; $F31B ........
			defb $00,$96,$00,$8E,$00,$86,$00,$7E				; $F323 .......~
			defb $00,$77,$00,$70,$00,$6A,$00,$64				; $F32B .w.p.j.d
			defb $00,$5E,$00,$59,$00,$54,$00,$4F				; $F333 .^.Y.T.O
			defb $00,$4B,$00,$47,$00,$43,$00,$3F				; $F33B .K.G.C.?
			defb $00,$3C,$00,$38,$00,$35,$00,$32				; $F343 .<.8.5.2
			defb $00,$2F,$00,$2D,$00,$2A,$00,$28				; $F34B ./.-.*.(
			defb $00,$00,$00,$7C,$01,$80,$01,$8A				; $F353 ...|....
			defb $01,$94,$01,$95,$01,$9C,$01,$A9				; $F35B ........
			defb $01,$B6,$01,$C3,$01,$CD,$01,$CE				; $F363 ........
			defb $01,$CF,$01,$D9,$01,$E3,$01,$ED				; $F36B ........
			defb $01,$F7,$01,$01,$02,$0B,$02,$12				; $F373 ........
			defb $02,$28,$02,$2C,$02,$30,$02,$3A				; $F37B .(.,.0.:
			defb $02,$47,$02,$48,$02,$58,$02,$5F				; $F383 .G.H.X._
			defb $02,$66,$02,$67,$02,$6B,$02,$78				; $F38B .f.g.k.x
			defb $02,$7F,$02,$80,$02,$81,$02,$82				; $F393 ........
			defb $02,$8C,$02,$A5,$02,$BE,$02,$CE				; $F39B ........
			defb $02,$00,$00,$DA,$02,$E0,$02,$9C				; $F3A3 ........
			defb $03,$A5,$03,$B0,$03,$E5,$03,$6E				; $F3AB .......n
			defb $04,$F1,$04,$18,$05,$3F,$05,$6C				; $F3B3 .....?.l
			defb $05,$77,$05,$7C,$05,$97,$05,$1A				; $F3BB .w.|....
			defb $06,$1D,$06,$32,$06,$C3,$06,$E0				; $F3C3 ...2....
			defb $06,$F9,$06,$14,$07,$87,$07,$8C				; $F3CB ........
			defb $07,$96,$07,$A4,$07,$AE,$07,$B8				; $F3D3 ........
			defb $07,$C8,$07,$D8,$07,$E2,$07,$F0				; $F3DB ........
			defb $07,$FE,$07,$08,$08,$18,$08,$2A				; $F3E3 .......*
			defb $08,$38,$08,$3B,$08,$48,$08,$5B				; $F3EB .8.;.H.[
			defb $08,$94,$08,$A7,$08,$C5,$08,$E2				; $F3F3 ........
			defb $08,$4A,$1A,$58,$1A,$62,$1A,$AE				; $F3FB .J.X.b..
			defb $1A,$CA,$1A,$E5,$1A,$00,$00,$C8				; $F403 ........
			defb $80,$C8,$FF,$01,$81,$0D,$09,$7F				; $F40B ........
			defb $09,$C8,$80,$C8,$FF,$01,$8D,$01				; $F413 ........
			defb $01,$7F,$04,$01,$77,$01,$FF,$FF				; $F41B ....w...
			defb $01,$87,$01,$C8,$80,$C8,$FF,$01				; $F423 ........
			defb $8C,$01,$0A,$7F,$08,$14,$7F,$04				; $F42B ........
			defb $C8,$80,$C8,$FF,$01,$8D,$01,$02				; $F433 ........
			defb $7F,$07,$0C,$7F,$06,$C8,$80,$C8				; $F43B ........
			defb $FF,$01,$8D,$01,$01,$7F,$0B,$09				; $F443 ........
			defb $7F,$02,$C8,$80,$C8,$FF,$01,$88				; $F44B ........
			defb $01,$01,$82,$01,$C8,$80,$C8,$FF				; $F453 ........
			defb $FF,$FF,$01,$77,$01,$01,$83,$03				; $F45B ...w....
			defb $C8,$80,$C8,$FF,$02,$81,$02,$02				; $F463 ........
			defb $7F,$03,$02,$81,$01,$FF,$01,$81				; $F46B ........
			defb $02,$01,$7F,$04,$01,$81,$02,$FF				; $F473 ........
			defb $01,$89,$01,$01,$79,$01,$C8,$80				; $F47B ....y...
			defb $C8,$FF,$01,$82,$02,$01,$7E,$04				; $F483 ......~.
			defb $01,$82,$02,$FF,$01,$87,$03,$01				; $F48B ........
			defb $79,$06,$01,$87,$03,$FF,$01,$88				; $F493 y.......
			defb $0A,$01,$96,$C8,$FF,$01,$93,$01				; $F49B ........
			defb $01,$6D,$01,$01,$7F,$02,$01,$81				; $F4A3 .m......
			defb $04,$01,$7F,$04,$01,$81,$02,$C8				; $F4AB ........
			defb $80,$C8,$FF,$02,$81,$C8,$FF,$02				; $F4B3 ........
			defb $7F,$C8,$FF,$01,$80,$01,$01,$B4				; $F4BB ........
			defb $01,$C8,$80,$C8,$FF,$01,$80,$01				; $F4C3 ........
			defb $01,$8B,$01,$01,$B4,$01,$C8,$80				; $F4CB ........
			defb $C8,$FF,$FF,$01,$85,$03,$01,$71				; $F4D3 .......q
			defb $01,$01,$80,$01,$01,$8F,$01,$01				; $F4DB ........
			defb $7B,$03,$FF,$01,$85,$03,$01,$71				; $F4E3 {......q
			defb $01,$FF,$01,$96,$01,$02,$79,$02				; $F4EB ......y.
			defb $FF,$FF,$01,$A1,$C8,$FF,$01,$85				; $F4F3 ........
			defb $03,$09,$7D,$04,$0E,$7F,$03,$C8				; $F4FB ..}.....
			defb $80,$C8,$FF,$01,$84,$01,$01,$7F				; $F503 ........
			defb $01,$FF,$FF,$FF,$FF,$02,$8D,$01				; $F50B ........
			defb $02,$67,$01,$C8,$80,$C8,$FF,$01				; $F513 .g......
			defb $8F,$01,$08,$80,$01,$02,$7F,$01				; $F51B ........
			defb $04,$7F,$01,$07,$7F,$02,$0C,$7F				; $F523 ........
			defb $04,$16,$7F,$07,$C8,$80,$C8,$FF				; $F52B ........
			defb $01,$79,$01,$01,$8B,$01,$01,$6F				; $F533 .y.....o
			defb $01,$01,$9B,$01,$01,$5B,$01,$01				; $F53B .....[..
			defb $AF,$01,$01,$47,$01,$01,$C3,$01				; $F543 ...G....
			defb $FF,$01,$8F,$01,$01,$7A,$01,$01				; $F54B .....z..
			defb $86,$01,$01,$7F,$09,$08,$7F,$06				; $F553 ........
			defb $FF,$01,$99,$03,$01,$79,$07,$0A				; $F55B .....y..
			defb $7E,$C8,$FF,$01,$9B,$E1,$FF,$FF				; $F563 ~.......
			defb $FF,$FF,$AF,$01,$E8,$00,$00,$30				; $F56B .......0
			defb $E5,$04,$02,$02,$E5,$04,$02,$03				; $F573 ........
			defb $E5,$04,$02,$08,$00,$54,$02,$04				; $F57B .....T..
			defb $E5,$05,$02,$04,$E5,$06,$02,$04				; $F583 ........
			defb $E5,$05,$02,$04,$E5,$06,$02,$00				; $F58B ........
			defb $E4,$06,$00,$06,$E4,$06,$00,$06				; $F593 ........
			defb $E5,$0D,$E5,$10,$E5,$0D,$E5,$10				; $F59B ........
			defb $E5,$08,$E5,$08,$E5,$08,$E5,$0C				; $F5A3 ........
			defb $02,$04,$E5,$05,$02,$04,$E5,$06				; $F5AB ........
			defb $02,$04,$E5,$05,$02,$04,$E5,$06				; $F5B3 ........
			defb $02,$00,$F0,$00,$E4,$06,$3A,$06				; $F5BB ......:.
			defb $E4,$06,$39,$06,$E5,$0D,$E5,$10				; $F5C3 ..9.....
			defb $E5,$0D,$E5,$10,$02,$25,$E5,$26				; $F5CB .....%.&
			defb $E4,$06,$00,$06,$E4,$06,$00,$06				; $F5D3 ........
			defb $E5,$09,$E5,$07,$E5,$09,$E5,$08				; $F5DB ........
			defb $E6,$13,$02,$13,$E5,$14,$E4,$01				; $F5E3 ........
			defb $00,$0C,$02,$13,$E5,$14,$E4,$01				; $F5EB ........
			defb $00,$0C,$E6,$15,$02,$15,$E5,$14				; $F5F3 ........
			defb $E4,$01,$00,$0C,$02,$15,$E5,$14				; $F5FB ........
			defb $02,$0F,$E5,$05,$02,$0F,$E5,$05				; $F603 ........
			defb $02,$0F,$E5,$05,$02,$0F,$E5,$05				; $F60B ........
			defb $E4,$06,$00,$06,$E4,$06,$00,$06				; $F613 ........
			defb $E5,$0D,$E5,$0D,$E5,$10,$02,$25				; $F61B .......%
			defb $E5,$26,$E5,$07,$E5,$0B,$FF,$E8				; $F623 .&......
			defb $00,$00,$60,$E5,$07,$E5,$07,$FF				; $F62B ..`.....
			defb $E5,$09,$E8,$00,$00,$30,$E5,$07				; $F633 .....0..
			defb $E5,$07,$FF,$E6,$07,$E8,$02,$E9				; $F63B ........
			defb $0D,$F0,$00,$1F,$0C,$1F,$0C,$22				; $F643 ......."
			defb $0C,$24,$0C,$26,$0C,$F0,$0F,$26				; $F64B .$.&...&
			defb $0C,$F0,$0E,$26,$0C,$26,$0C,$F0				; $F653 ...&.&..
			defb $10,$22,$0C,$F0,$00,$22,$0C,$16				; $F65B ."..."..
			defb $0C,$22,$0C,$16,$0C,$F0,$10,$24				; $F663 .".....$
			defb $0C,$F0,$00,$18,$0C,$24,$0C,$FF				; $F66B .....$..
			defb $E6,$07,$E8,$06,$F0,$18,$03,$27				; $F673 .......'
			defb $EA,$16,$E4,$01,$30,$06,$E4,$01				; $F67B ....0...
			defb $31,$06,$E4,$01,$32,$06,$E4,$01				; $F683 1...2...
			defb $30,$06,$E4,$01,$32,$06,$E4,$01				; $F68B 0...2...
			defb $35,$06,$EA,$00,$E4,$06,$3A,$0C				; $F693 5.....:.
			defb $EA,$16,$E4,$01,$3C,$06,$E4,$01				; $F69B ....<...
			defb $39,$06,$E4,$01,$00,$06,$E4,$01				; $F6A3 9.......
			defb $00,$06,$E4,$01,$00,$06,$E4,$01				; $F6AB ........
			defb $00,$06,$EA,$00,$E4,$06,$00,$0C				; $F6B3 ........
			defb $EA,$16,$E4,$01,$00,$06,$E4,$01				; $F6BB ........
			defb $00,$06,$E4,$01,$00,$06,$E4,$01				; $F6C3 ........
			defb $00,$06,$E4,$01,$2E,$06,$E4,$01				; $F6CB ........
			defb $00,$06,$EA,$00,$E4,$06,$30,$0C				; $F6D3 ......0.
			defb $EA,$16,$E4,$01,$32,$06,$E4,$01				; $F6DB ....2...
			defb $30,$06,$E4,$01,$00,$06,$E4,$01				; $F6E3 0.......
			defb $00,$06,$E4,$01,$00,$06,$E4,$01				; $F6EB ........
			defb $00,$06,$EA,$00,$E4,$06,$00,$0C				; $F6F3 ........
			defb $FF,$03,$27,$EA,$16,$E4,$01,$30				; $F6FB ..'....0
			defb $06,$E4,$01,$31,$06,$E4,$01,$32				; $F703 ...1...2
			defb $06,$E4,$01,$30,$06,$E4,$01,$32				; $F70B ...0...2
			defb $06,$E4,$01,$35,$06,$EA,$00,$E4				; $F713 ...5....
			defb $06,$3A,$0C,$EA,$16,$E4,$01,$3C				; $F71B .:.....<
			defb $06,$E4,$01,$39,$06,$E4,$01,$00				; $F723 ...9....
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F72B ........
			defb $06,$E4,$01,$00,$06,$EA,$00,$E4				; $F733 ........
			defb $06,$39,$0C,$EA,$16,$E4,$01,$35				; $F73B .9.....5
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F743 ........
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F74B ........
			defb $06,$E4,$01,$00,$06,$EA,$00,$E4				; $F753 ........
			defb $06,$37,$0C,$EA,$16,$E4,$01,$34				; $F75B .7.....4
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F763 ........
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F76B ........
			defb $06,$E4,$01,$00,$06,$EA,$00,$E4				; $F773 ........
			defb $06,$00,$0C,$FF,$E8,$07,$EA,$15				; $F77B ........
			defb $03,$00,$E4,$01,$00,$06,$E4,$01				; $F783 ........
			defb $00,$06,$E4,$01,$00,$06,$E4,$01				; $F78B ........
			defb $00,$06,$EA,$00,$E4,$06,$00,$0C				; $F793 ........
			defb $EA,$15,$E4,$01,$00,$06,$E4,$01				; $F79B ........
			defb $00,$06,$FF,$E8,$07,$EA,$16,$03				; $F7A3 ........
			defb $00,$E4,$01,$00,$06,$E4,$01,$00				; $F7AB ........
			defb $06,$E4,$01,$00,$06,$E4,$01,$00				; $F7B3 ........
			defb $06,$EA,$00,$E4,$06,$00,$0C,$EA				; $F7BB ........
			defb $16,$E4,$01,$00,$06,$E4,$01,$00				; $F7C3 ........
			defb $06,$FF,$E6,$13,$E8,$07,$EA,$00				; $F7CB ........
			defb $E9,$00,$03,$00,$EA,$16,$E4,$01				; $F7D3 ........
			defb $43,$06,$E4,$01,$41,$06,$E4,$0E				; $F7DB C...A...
			defb $3C,$06,$E4,$01,$3E,$06,$EA,$00				; $F7E3 <...>...
			defb $E4,$06,$00,$0C,$EA,$16,$E4,$06				; $F7EB ........
			defb $00,$06,$E4,$06,$00,$06,$FF,$E6				; $F7F3 ........
			defb $13,$E8,$04,$E9,$10,$00,$0C,$48				; $F7FB .......H
			defb $A8,$E1,$00,$F0,$00,$F0,$FF,$E8				; $F803 ........
			defb $07,$EA,$16,$E4,$01,$00,$06,$E4				; $F80B ........
			defb $01,$00,$06,$E4,$01,$00,$06,$E4				; $F813 ........
			defb $01,$00,$06,$EA,$00,$E4,$06,$00				; $F81B ........
			defb $0C,$FF,$E6,$07,$E8,$06,$E9,$0F				; $F823 ........
			defb $F0,$18,$1F,$06,$F0,$00,$1F,$06				; $F82B ........
			defb $F0,$18,$1F,$06,$F0,$00,$1F,$06				; $F833 ........
			defb $EA,$14,$E4,$06,$00,$06,$1F,$06				; $F83B ........
			defb $F0,$18,$1F,$06,$F0,$00,$1F,$06				; $F843 ........
			defb $F0,$1B,$1E,$06,$F0,$00,$1E,$06				; $F84B ........
			defb $F0,$1B,$1E,$06,$F0,$00,$1E,$06				; $F853 ........
			defb $E4,$06,$00,$06,$1E,$06,$F0,$1B				; $F85B ........
			defb $1E,$06,$F0,$00,$1E,$06,$F0,$1B				; $F863 ........
			defb $1F,$06,$F0,$00,$1F,$06,$F0,$1B				; $F86B ........
			defb $1F,$06,$F0,$00,$1F,$06,$EA,$14				; $F873 ........
			defb $E4,$06,$00,$06,$1F,$06,$F0,$1B				; $F87B ........
			defb $1F,$06,$F0,$00,$1F,$06,$F0,$1D				; $F883 ........
			defb $21,$06,$F0,$00,$21,$06,$F0,$1D				; $F88B !...!...
			defb $21,$06,$F0,$00,$21,$06,$E4,$06				; $F893 !...!...
			defb $21,$06,$21,$06,$F0,$1B,$21,$06				; $F89B !.!...!.
			defb $F0,$00,$21,$06,$FF,$00,$06,$FF				; $F8A3 ..!.....
			defb $E4,$06,$00,$06,$E4,$06,$00,$06				; $F8AB ........
			defb $E5,$0D,$E8,$04,$3C,$30,$48,$90				; $F8B3 ....<0H.
			defb $3C,$30,$48,$90,$FF,$02,$11,$F0				; $F8BB <0H.....
			defb $18,$22,$06,$F0,$00,$22,$06,$F0				; $F8C3 ."..."..
			defb $18,$22,$06,$F0,$00,$22,$06,$EA				; $F8CB ."..."..
			defb $01,$E4,$06,$00,$06,$22,$06,$F0				; $F8D3 ....."..
			defb $18,$22,$06,$F0,$00,$22,$06,$F0				; $F8DB ."..."..
			defb $28,$1D,$06,$F0,$00,$1D,$06,$F0				; $F8E3 (.......
			defb $28,$1D,$06,$F0,$00,$1D,$06,$E4				; $F8EB (.......
			defb $06,$00,$06,$1D,$06,$F0,$29,$E4				; $F8F3 ......).
			defb $06,$1D,$06,$F0,$00,$1D,$06,$02				; $F8FB ........
			defb $12,$EA,$01,$F0,$21,$E4,$01,$24				; $F903 ....!..$
			defb $06,$F0,$00,$E4,$01,$24,$06,$F0				; $F90B .....$..
			defb $1F,$E4,$01,$24,$06,$F0,$00,$E4				; $F913 ...$....
			defb $01,$24,$06,$F0,$1F,$E4,$06,$24				; $F91B .$.....$
			defb $06,$F0,$00,$24,$06,$24,$06,$24				; $F923 ...$.$.$
			defb $06,$F0,$1F,$E4,$0E,$24,$06,$E4				; $F92B .....$..
			defb $0A,$24,$06,$E4,$07,$24,$06,$E4				; $F933 .$...$..
			defb $04,$24,$06,$F0,$00,$E4,$01,$24				; $F93B .$.....$
			defb $06,$E4,$04,$24,$06,$E4,$0A,$29				; $F943 ...$...)
			defb $06,$E4,$0E,$2B,$06,$FF,$E6,$1F				; $F94B ...+....
			defb $E8,$08,$E9,$0C,$00,$0C,$22,$0C				; $F953 ......".
			defb $24,$0C,$26,$0C,$2D,$06,$00,$06				; $F95B $.&.-...
			defb $2D,$06,$00,$06,$2D,$0C,$2E,$0C				; $F963 -...-...
			defb $00,$30,$E1,$E6,$1F,$E8,$08,$E9				; $F96B .0......
			defb $0C,$2D,$0C,$2B,$06,$00,$06,$2B				; $F973 .-.+...+
			defb $06,$00,$06,$2B,$24,$E9,$11,$37				; $F97B ...+$..7
			defb $24,$00,$60,$E1,$E8,$02,$E9,$0D				; $F983 $.`.....
			defb $24,$18,$27,$18,$2B,$18,$2E,$18				; $F98B $.'.+...
			defb $27,$24,$28,$0C,$29,$18,$2D,$0C				; $F993 '$(.).-.
			defb $2E,$06,$30,$06,$00,$60,$E1,$E8				; $F99B ..0..`..
			defb $05,$F0,$00,$E9,$0C,$03,$27,$EA				; $F9A3 ......'.
			defb $16,$E4,$01,$30,$06,$2E,$06,$E4				; $F9AB ...0....
			defb $01,$2C,$06,$29,$06,$EA,$00,$E4				; $F9B3 .,.)....
			defb $06,$27,$06,$24,$06,$E4,$01,$22				; $F9BB .'.$..."
			defb $06,$24,$06,$EA,$16,$E4,$01,$27				; $F9C3 .$.....'
			defb $06,$00,$06,$E4,$01,$24,$06,$00				; $F9CB .....$..
			defb $06,$EA,$00,$E4,$06,$2B,$06,$00				; $F9D3 .....+..
			defb $06,$E4,$01,$27,$06,$27,$06,$EA				; $F9DB ...'.'..
			defb $16,$E4,$01,$00,$06,$00,$06,$E4				; $F9E3 ........
			defb $01,$00,$06,$00,$06,$EA,$00,$E4				; $F9EB ........
			defb $06,$29,$06,$00,$06,$E4,$01,$26				; $F9F3 .).....&
			defb $06,$26,$06,$EA,$16,$E4,$01,$00				; $F9FB .&......
			defb $06,$00,$06,$E4,$01,$00,$06,$00				; $FA03 ........
			defb $06,$EA,$00,$E4,$06,$00,$06,$00				; $FA0B ........
			defb $06,$FF,$E6,$02,$E5,$13,$FF,$02				; $FA13 ........
			defb $F4,$0F,$E8,$19,$E9,$18,$5D,$48				; $FA1B ......]H
			defb $E1,$02,$F4,$0F,$E8,$01,$F0,$3C				; $FA23 .......<
			defb $EA,$05,$E4,$02,$24,$48,$E1,$02				; $FA2B ....$H..
			defb $F4,$0F,$E8,$1D,$E9,$1E,$4F,$4D				; $FA33 ......OM
			defb $E1,$03,$F4,$0F,$E8,$06,$E9,$19				; $FA3B ........
			defb $53,$0F,$E1,$03,$F4,$0F,$E8,$07				; $FA43 S.......
			defb $E9,$01,$EA,$01,$E4,$07,$5E,$10				; $FA4B ......^.
			defb $EA,$16,$E1,$03,$F4,$0F,$E8,$05				; $FA53 ........
			defb $E9,$1C,$EA,$27,$E4,$03,$4F,$14				; $FA5B ...'..O.
			defb $EA,$16,$E1,$02,$F4,$0F,$E8,$15				; $FA63 ........
			defb $E9,$10,$43,$14,$E1,$03,$F4,$0F				; $FA6B ..C.....
			defb $E2,$0F,$E8,$25,$E9,$26,$E6,$3E				; $FA73 ...%.&.>
			defb $E3,$19,$E1,$02,$F4,$0F,$E8,$23				; $FA7B .......#
			defb $E9,$24,$EA,$00,$E4,$0E,$56,$63				; $FA83 .$....Vc
			defb $E1,$03,$F4,$0F,$E8,$12,$E9,$24				; $FA8B .......$
			defb $61,$20,$E1,$02,$F4,$0F,$E8,$1D				; $FA93 a ......
			defb $E9,$1A,$EA,$0B,$E4,$0C,$39,$40				; $FA9B ......9@
			defb $E9,$0C,$E1,$03,$F4,$0F,$E8,$06				; $FAA3 ........
			defb $E9,$22,$EA,$00,$E4,$0E,$5E,$02				; $FAAB ."....^.
			defb $5E,$10,$EA,$16,$E1,$01,$F4,$0F				; $FAB3 ^.......
			defb $02,$24,$00,$12,$03,$24,$00,$12				; $FABB .$...$..
			defb $E5,$24,$E1,$00,$03,$E1,$F4,$0F				; $FAC3 .$......
			defb $E8,$02,$E9,$1A,$EA,$15,$E4,$07				; $FACB ........
			defb $5E,$5A,$E1,$E6,$07,$E8,$02,$E9				; $FAD3 ^Z......
			defb $0F,$20,$48,$22,$18,$23,$48,$E8				; $FADB . H".#H.
			defb $05,$25,$78,$E5,$0A,$E1,$E6,$07				; $FAE3 .%x.....
			defb $E8,$07,$F0,$0C,$37,$06,$37,$06				; $FAEB ....7.7.
			defb $35,$0C,$EA,$16,$E4,$04,$33,$0C				; $FAF3 5.....3.
			defb $30,$18,$32,$0C,$E4,$04,$33,$0C				; $FAFB 0.2...3.
			defb $E4,$04,$35,$0C,$36,$06,$36,$06				; $FB03 ..5.6.6.
			defb $35,$0C,$E4,$04,$33,$0C,$2F,$18				; $FB0B 5...3./.
			defb $31,$0C,$EA,$14,$E4,$06,$33,$0C				; $FB13 1.....3.
			defb $E8,$05,$35,$60,$F0,$00,$FF,$E6				; $FB1B ..5`....
			defb $37,$E8,$0E,$E9,$00,$E2,$1F,$E3				; $FB23 7.......
			defb $06,$13,$06,$E3,$06,$1F,$06,$E3				; $FB2B ........
			defb $06,$FF,$01,$F4,$0F,$E8,$08,$E9				; $FB33 ........
			defb $0F,$E2,$0E,$EA,$13,$02,$2A,$03				; $FB3B ......*.
			defb $29,$E6,$1E,$E3,$18,$E4,$01,$00				; $FB43 ).......
			defb $18,$E3,$18,$E4,$01,$00,$18,$FF				; $FB4B ........
			defb $F4,$0F,$E8,$08,$E9,$0C,$E6,$31				; $FB53 .......1
			defb $E3,$06,$E6,$36,$E3,$06,$E6,$38				; $FB5B ...6...8
			defb $E3,$06,$E6,$3A,$E3,$06,$E6,$38				; $FB63 ...:...8
			defb $E3,$2A,$00,$0C,$FF,$F4,$0F,$E8				; $FB6B .*......
			defb $08,$E9,$0C,$E6,$1E,$E3,$18,$E3				; $FB73 ........
			defb $18,$E3,$18,$E3,$18,$FF,$E6,$34				; $FB7B .......4
			defb $1A,$00,$00                                    ; $FB83 ...

			LD HL,$FC4E				; $FB86
			LD ($FC47),HL				; $FB89
			RET				; $FB8C

			JP $FB97				; $FB8D

			IN A,(C)				; $FB90
			AND $1F				; $FB92
			CP $1F				; $FB94
			RET				; $FB96

			LD BC,$FEFE				; $FB97
			CALL $FB90				; $FB9A
			RET NZ				; $FB9D
			LD BC,$FDFE				; $FB9E
			CALL $FB90				; $FBA1
			RET NZ				; $FBA4
			LD BC,$FBFE				; $FBA5
			CALL $FB90				; $FBA8
			RET NZ				; $FBAB
			LD BC,$F7FE				; $FBAC
			CALL $FB90				; $FBAF
			RET NZ				; $FBB2
			LD BC,$EFFE				; $FBB3
			CALL $FB90				; $FBB6
			RET NZ				; $FBB9
			LD BC,$DFFE				; $FBBA
			CALL $FB90				; $FBBD
			RET NZ				; $FBC0
			LD BC,$BFFE				; $FBC1
			CALL $FB90				; $FBC4
			RET NZ				; $FBC7
			LD BC,$7FFE				; $FBC8
			CALL $FB90				; $FBCB
			RET NZ				; $FBCE
			XOR A				; $FBCF
			LD ($FC49),A				; $FBD0
			LD HL,($FC47)				; $FBD3
			LD A,(HL)				; $FBD6
			CP $FF				; $FBD7
			JP NZ,$FBE0				; $FBD9
			LD HL,$FC4E				; $FBDC
			LD A,(HL)				; $FBDF
			LD ($FC45),A				; $FBE0
			INC HL				; $FBE3
			LD A,(HL)				; $FBE4
			LD ($FC46),A				; $FBE5
			INC HL				; $FBE8
			LD D,(HL)				; $FBE9
			LD E,$00				; $FBEA
			INC HL				; $FBEC
			LD ($FC47),HL				; $FBED
			LD A,($5C48)				; $FBF0
			RRA				; $FBF3
			RRA				; $FBF4
			RRA				; $FBF5
			AND $07				; $FBF6
			LD ($FC43),A				; $FBF8
			LD C,A				; $FBFB
			LD A,($FC45)				; $FBFC
			OR A				; $FBFF
			LD A,C				; $FC00
			JR Z,$FC05				; $FC01
			OR $10				; $FC03
			LD ($FC44),A				; $FC05
			LD HL,$FC45				; $FC08
			LD B,(HL)				; $FC0B
			DEC DE				; $FC0C
			LD A,D				; $FC0D
			OR E				; $FC0E
			JP Z,$FC40				; $FC0F
			DJNZ $FC0C				; $FC12
			LD A,($FC43)				; $FC14
			OUT ($FE),A				; $FC17
			LD B,(HL)				; $FC19
			DEC DE				; $FC1A
			LD A,D				; $FC1B
			OR E				; $FC1C
			JP Z,$FC40				; $FC1D
			DJNZ $FC1A				; $FC20
			LD A,($FC44)				; $FC22
			OUT ($FE),A				; $FC25
			LD A,($FC49)				; $FC27
			INC A				; $FC2A
			CP $08				; $FC2B
			JR Z,$FC38				; $FC2D
			CP $10				; $FC2F
			JR Z,$FC3B				; $FC31
			LD ($FC49),A				; $FC33
			JR $FC0B				; $FC36

			INC HL				; $FC38
			JR $FC33				; $FC39

			DEC HL				; $FC3B
			LD A,$00				; $FC3C
			JR $FC33				; $FC3E

			JP $FB8D				; $FC40

			defb $07,$17,$49,$4A,$08,$FD,$09,$00				; $FC43 ..IJ....
			defb $00,$00,$1A,$DD,$DD,$2A,$AF,$AF				; $FC4B .....*..
			defb $2A,$95,$95,$FC,$A6,$A6,$2A,$84				; $FC53 *.....*.
			defb $84,$2A,$6E,$6E,$FC,$84,$84,$2A				; $FC5B .*nn...*
			defb $6E,$6E,$2A,$62,$62,$2A,$57,$57				; $FC63 nn*bb*WW
			defb $2A,$62,$62,$2A,$6E,$6E,$2A,$84				; $FC6B *bb*nn*.
			defb $84,$2A,$95,$95,$2A,$AF,$AF,$2A				; $FC73 .*..*..*
			defb $95,$95,$2A,$84,$84,$FC,$6E,$6F				; $FC7B ..*...no
			defb $2A,$62,$63,$2A,$57,$58,$FC,$6E				; $FC83 *bc*WX.n
			defb $6F,$2A,$57,$58,$2A,$49,$4A,$FC				; $FC8B o*WX*IJ.
			defb $57,$58,$2A,$49,$4A,$2A,$41,$42				; $FC93 WX*IJ*AB
			defb $2A,$36,$37,$2A,$41,$42,$2A,$49				; $FC9B *67*AB*I
			defb $4A,$2A,$57,$58,$2A,$41,$42,$2A				; $FCA3 J*WX*AB*
			defb $49,$4A,$2A,$2B,$2C,$2A,$30,$31				; $FCAB IJ*+,*01
			defb $2A,$30,$31,$2A,$30,$31,$2A,$30				; $FCB3 *01*01*0
			defb $31,$2A,$30,$31,$2A,$00,$FF,$2A				; $FCBB 1*01*..*
			defb $28,$29,$2A,$2B,$2C,$2A,$30,$31				; $FCC3 ()*+,*01
			defb $9E,$00,$FF,$0A,$30,$31,$9E,$00				; $FCCB ....01..
			defb $FF,$0A,$30,$31,$2A,$36,$37,$2A				; $FCD3 ..01*67*
			defb $30,$31,$2A,$2B,$2C,$2A,$30,$31				; $FCDB 01*+,*01
			defb $2A,$00,$FF,$2A,$49,$4A,$2A,$39				; $FCE3 *..*IJ*9
			defb $3A,$2A,$36,$37,$2A,$39,$3A,$2A				; $FCEB :*67*9:*
			defb $49,$4A,$2A,$52,$53,$2A,$41,$42				; $FCF3 IJ*RS*AB
			defb $2A,$36,$37,$2A,$41,$42,$2A,$57				; $FCFB *67*AB*W
			defb $58,$2A,$49,$4A,$2A,$39,$3A,$2A				; $FD03 X*IJ*9:*
			defb $49,$4A,$2A,$62,$63,$2A,$52,$53				; $FD0B IJ*bc*RS
			defb $2A,$41,$42,$2A,$52,$53,$2A,$76				; $FD13 *AB*RS*v
			defb $77,$2A,$62,$63,$2A,$52,$53,$2A				; $FD1B w*bc*RS*
			defb $62,$63,$2A,$76,$77,$2A,$62,$63				; $FD23 bc*vw*bc
			defb $2A,$52,$53,$2A,$62,$63,$2A,$76				; $FD2B *RS*bc*v
			defb $77,$2A,$62,$63,$2A,$45,$46,$2A				; $FD33 w*bc*EF*
			defb $57,$58,$2A,$76,$77,$2A,$57,$58				; $FD3B WX*vw*WX
			defb $2A,$45,$46,$2A,$57,$58,$2A,$76				; $FD43 *EF*WX*v
			defb $77,$2A,$41,$42,$54,$57,$58,$54				; $FD4B w*ABTWXT
			defb $52,$53,$54,$41,$42,$54,$49,$4A				; $FD53 RSTABTIJ
			defb $9E,$00,$FF,$0A,$49,$4A,$54,$00				; $FD5B ....IJT.
			defb $FF,$2A,$49,$4A,$2A,$52,$6D,$54				; $FD63 .*IJ*RmT
			defb $62,$83,$54,$57,$74,$54,$52,$6D				; $FD6B b.TWtTRm
			defb $54,$57,$74,$9E,$00,$FF,$0A,$57				; $FD73 TWt....W
			defb $74,$9E,$00,$FF,$0A,$45,$44,$15				; $FD7B t....ED.
			defb $41,$40,$15,$45,$44,$15,$41,$40				; $FD83 A@.ED.A@
			defb $15,$45,$44,$15,$41,$40,$15,$45				; $FD8B .ED.A@.E
			defb $44,$15,$41,$40,$15,$45,$44,$15				; $FD93 D.A@.ED.
			defb $41,$40,$15,$45,$44,$15,$41,$40				; $FD9B A@.ED.A@
			defb $15,$45,$44,$15,$41,$40,$15,$45				; $FDA3 .ED.A@.E
			defb $44,$15,$41,$40,$15,$45,$44,$54				; $FDAB D.A@.EDT
			defb $41,$40,$54,$39,$38,$54,$45,$44				; $FDB3 A@T98TED
			defb $54,$33,$32,$9E,$00,$FF,$0A,$33				; $FDBB T32....3
			defb $32,$74,$00,$FF,$0A,$33,$32,$20				; $FDC3 2t...32 
			defb $00,$FF,$0A,$33,$32,$A8,$00,$FF				; $FDCB ...32...
			defb $A8,$00,$FF,$A8,$00,$FF,$A8,$FF				; $FDD3 ........
			defb $FF,$00,$00,$00,$00,$00,$00,$00				; $FDDB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FDE3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FDEB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FDF3 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FDFB ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE03 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE0B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE13 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE1B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE23 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE2B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE33 ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE3B ........
			defb $00,$00,$00,$00,$00,$00,$00,$00				; $FE43 ........
			defb $00,$F5,$3E,$FF,$D3,$7F,$D3,$3F				; $FE4B ..>....?
			defb $3E,$3F,$D3,$7F,$3C,$32,$86,$FE				; $FE53 >?..<2..
			defb $3E,$FF,$D3,$5F,$D3,$5F,$F1                    ; $FE5B >.._._.

			DI				; $FE62
			LD ($FF9C),HL				; $FE63
			LD ($FFA0),SP				; $FE66
			POP HL				; $FE6A
			LD ($FF9E),HL				; $FE6B
			LD SP,$FF9C				; $FE6E
			PUSH DE				; $FE71
			PUSH BC				; $FE72
			EXX				; $FE73
			PUSH HL				; $FE74
			PUSH DE				; $FE75
			PUSH BC				; $FE76
			PUSH IX				; $FE77
			PUSH IY				; $FE79
			PUSH AF				; $FE7B
			LD A,I				; $FE7C
			LD H,A				; $FE7E
			LD A,R				; $FE7F
			LD L,A				; $FE81
			PUSH HL				; $FE82
			EX AF,AF'				; $FE83
			PUSH AF				; $FE84
			LD D,$40				; $FE85
			CALL $FF67				; $FE87
			LD A,E				; $FE8A
			CP $B4				; $FE8B
			JR Z,$FEBC				; $FE8D
			CP $BA				; $FE8F
			JP Z,$FF2D				; $FE91
			CP $B8				; $FE94
			JR Z,$FEE2				; $FE96
			CP $B9				; $FE98
			JR Z,$FEEA				; $FE9A
			CP $B7				; $FE9C
			JR Z,$FED8				; $FE9E
			CP $B6				; $FEA0
			JR Z,$FEF2				; $FEA2
			CP $B5				; $FEA4
			JR NZ,$FE87				; $FEA6
			CALL $FF67				; $FEA8
			LD H,E				; $FEAB
			CALL $FF67				; $FEAC
			LD L,E				; $FEAF
			LD ($FF62),HL				; $FEB0
			LD HL,$CD00				; $FEB3
			LD ($FF60),HL				; $FEB6
			JP $FF43				; $FEB9

			CALL $FF67				; $FEBC
			LD H,E				; $FEBF
			CALL $FF67				; $FEC0
			LD L,E				; $FEC3
			CALL $FF67				; $FEC4
			LD B,E				; $FEC7
			CALL $FF67				; $FEC8
			LD C,E				; $FECB
			CALL $FF67				; $FECC
			LD (HL),E				; $FECF
			INC HL				; $FED0
			DEC BC				; $FED1
			LD A,B				; $FED2
			OR C				; $FED3
			JR NZ,$FECC				; $FED4
			JR $FE87				; $FED6

			CALL $FF67				; $FED8
			LD BC,$7FFD				; $FEDB
			OUT (C),E				; $FEDE
			JR $FE87				; $FEE0

			LD HL,$FF88				; $FEE2
			LD BC,$001A				; $FEE5
			JR $FF02				; $FEE8

			LD HL,$FF88				; $FEEA
			LD BC,$001A				; $FEED
			JR $FECC				; $FEF0

			CALL $FF67				; $FEF2
			LD H,E				; $FEF5
			CALL $FF67				; $FEF6
			LD L,E				; $FEF9
			CALL $FF67				; $FEFA
			LD B,E				; $FEFD
			CALL $FF67				; $FEFE
			LD C,E				; $FF01
			IN A,($3F)				; $FF02
			XOR D				; $FF04
			RRCA				; $FF05
			JR C,$FF02				; $FF06
			LD A,$FF				; $FF08
			OUT ($5F),A				; $FF0A
			INC A				; $FF0C
			OUT ($5F),A				; $FF0D
			LD A,D				; $FF0F
			XOR $41				; $FF10
			LD D,A				; $FF12
			LD A,(HL)				; $FF13
			CALL $FF77				; $FF14
			INC HL				; $FF17
			DEC BC				; $FF18
			LD A,B				; $FF19
			OR C				; $FF1A
			JR NZ,$FF13				; $FF1B
			DEC A				; $FF1D
			OUT ($5F),A				; $FF1E
			OUT ($5F),A				; $FF20
			LD A,D				; $FF22
			XOR $40				; $FF23
			OUT ($3F),A				; $FF25
			XOR $80				; $FF27
			LD D,A				; $FF29
			JP $FE87				; $FF2A

			CALL $FF67				; $FF2D
			LD L,E				; $FF30
			CALL $FF67				; $FF31
			LD H,E				; $FF34
			LD ($FF60),HL				; $FF35
			CALL $FF67				; $FF38
			LD L,E				; $FF3B
			CALL $C067				; $FF3C
			LD H,D				; $FF3F
			LD ($FF62),HL				; $FF40
			LD A,D				; $FF43
			LD ($FE86),A				; $FF44
			POP AF				; $FF47
			EX AF,AF'				; $FF48
			POP HL				; $FF49
			LD A,H				; $FF4A
			LD I,A				; $FF4B
			LD A,L				; $FF4D
			LD R,A				; $FF4E
			POP AF				; $FF50
			POP IY				; $FF51
			POP IX				; $FF53
			POP BC				; $FF55
			POP DE				; $FF56
			POP HL				; $FF57
			EXX				; $FF58
			POP BC				; $FF59
			POP DE				; $FF5A
			POP HL				; $FF5B
			LD SP,($FFA0)				; $FF5C
			NOP				; $FF60
			CALL $6501				; $FF61
			JP $FE62				; $FF64

			IN A,($3F)				; $FF67
			XOR D				; $FF69
			RRCA				; $FF6A
			JR C,$FF67				; $FF6B
			IN A,($1F)				; $FF6D
			LD E,A				; $FF6F
			LD A,D				; $FF70
			OUT ($3F),A				; $FF71
			XOR $81				; $FF73
			LD D,A				; $FF75
			RET				; $FF76

			OUT ($1F),A				; $FF77
			LD A,D				; $FF79
			OUT ($3F),A				; $FF7A
			XOR $81				; $FF7C
			LD D,A				; $FF7E
			IN A,($3F)				; $FF7F
			XOR D				; $FF81
			RRCA				; $FF82
			JR NC,$FF7F				; $FF83
			RET				; $FF85

			defb $AF,$FE,$44,$00,$75                            ; $FF86 0....D.u
			defb $3F,$54,$4C,$3A,$5C,$04,$5D,$21				; $FF8B ?TL:\.]!
			defb $17,$9B,$36,$58,$27,$4C,$FE,$43				; $FF93 ..6X'L.C
			defb $5D,$2B,$2D,$2B,$2D,$E8,$FF,$02				; $FF9B ]+-+-...
			defb $02,$42,$42,$3C,$00,$00,$44,$48				; $FFA3 .BB<..DH
			defb $70,$48,$44,$42,$00,$00,$40,$40				; $FFAB pHDB..@@
			defb $40,$40,$40,$7E,$00,$00,$42,$66				; $FFB3 @@@~..Bf
			defb $5A,$42,$42,$E0,$57,$F3,$0D,$CE				; $FFBB ZBB.W...
			defb $0B,$D3,$50,$CE,$0B,$D4,$50,$0D				; $FFC3 ..P...P.
			defb $17,$DC,$0A,$CE,$0B,$E6,$50,$DB				; $FFCB ......P.
			defb $02,$DB,$02,$7C,$38,$72,$5D,$4D				; $FFD3 ...|8r]M
			defb $00,$E1,$50,$06,$03,$07,$5C,$7C				; $FFDB ..P...\|
			defb $38,$C0,$57,$DB,$02,$7C,$38,$72				; $FFE3 8.W..|8r
			defb $5D,$4D,$00,$FF,$FF,$0E,$01,$00				; $FFEB ]M......
			defb $00,$51,$00,$92,$09,$3F,$05,$76				; $FFF3 .Q...?.v
			defb $1B,$03,$13,$3C                                ; $FFFB ...<

CODE2END equ $ ; Marker to show end of object code

#end ; terminate #target
