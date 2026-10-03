#!/bin/bash
#
# :dbolli:20140321 11:58:29 

Z80SRCDIR=~/dev/Z80\ src/cybernoid-zasm
Z80TAPFILENAME=cybernoid-tap.tap
PERLUTILSRCDIR=~/dev/perl-src

#cd "$Z80SRCDIR"
cd "$PERLUTILSRCDIR"

#perl $PERLUTILSRCDIR/hex-dump-file.pl $Z80TAPFILENAME
perl hex-dump-file.pl "$Z80SRCDIR/$Z80TAPFILENAME"

