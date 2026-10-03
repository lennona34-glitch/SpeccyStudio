#!/bin/bash
#
# :dbolli:20140422 13:57:16 Zasm Assembler

ZASMLOCATION=~/Applications/zasm-cocoa/bin

Z80EXECSRCDIR=~/dev/Z80\ src
Z80EXECNAME=cybernoid

cd "$Z80EXECSRCDIR/$Z80EXECNAME-zasm/"

$ZASMLOCATION/zasm -vw2 "$Z80EXECNAME-tap.zasm" -l "$Z80EXECNAME-tap.lis"

tapfilecreated=`find ./$Z80EXECNAME-tap.tap -type f -ctime -5s | grep $Z80EXECNAME-tap.tap`
#echo $tapfilecreated		# DEBUG
if [ "$tapfilecreated" != "" ]
then
    #./"$Z80EXECNAME"-tap-file-compare.sh
	
	open $Z80EXECNAME-tap.tap
fi
