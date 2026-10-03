#! /usr/bin/perl -w
use strict;
use warnings;

sub getDateTimeStamp {

    (my $second, my $minute, my $hour, my $dayOfMonth, my $month, my $yearOffset, my $dayOfWeek, my $dayOfYear, my $daylightSavings) = localtime();
    my $year = 1900 + $yearOffset;

    return sprintf( "%02d%02d%02d-%02d%02d%02d", $year, $month + 1, $dayOfMonth, $hour, $minute, $second );
}

sub getDateTimeString {

    (my $second, my $minute, my $hour, my $dayOfMonth, my $month, my $yearOffset, my $dayOfWeek, my $dayOfYear, my $daylightSavings) = localtime();
    my $year = 1900 + $yearOffset;

    return sprintf( "%02d\/%02d\/%02d %02d:%02d:%02d", $dayOfMonth, $month + 1, $year, $hour, $minute, $second );
}

sub getMessageTimeStamp {

	use Time::Piece;

	my $t = localtime;
	
	return $t->strftime( "%Y-%m-%dT%H:%M:%S.123" );

}

sub stringExistsInArray {			# derek.bolli 20121026 09:06:34 e.g. if ( stringExistsInArray( \@myarray, $mystring )
	
	my $searcharrayref = shift;
	my $searchstring = shift;
	
	my %searchhash = map { $_ => 1 } @{ $searcharrayref };			# derek.bolli 20121026 09:06:34 Convert array to hash as per http://stackoverflow.com/questions/2860226/how-can-i-check-if-a-perl-array-contains-a-particular-value
		
	return ( exists( $searchhash{$searchstring} ) );
	
}

sub indexOfStringInArray {		# as per http://stackoverflow.com/questions/3019708/simplest-way-to-match-array-of-strings-to-search-in-perl

	my $searcharrayref = shift;
	my $searchstring = shift;
	my $occurancenum = shift;
	
	my $found_index = -1;
	
	foreach my $index ( 0..@$searcharrayref ) {
#		our $log->info( "index = $index, comparing $searchstring to $$searcharrayref[$index]" );		# DEBUG
#		next if $searchstring !~ /$$searcharrayref[$index]/;		# commented out as grep needs target array to be escaped for search to succeed
		next if $searchstring ne $$searcharrayref[$index];
		$occurancenum--;
		if ( $occurancenum == 0 )
		{
			$found_index = $index;
			last; # quit the loop early
		}
	}

	return $found_index;

}

use Log::Log4perl;
use Config::IniFiles;

use File::Spec;
#use File::Path;

(my $currentscriptvolume, my $currentscriptdirectories, our $currentscriptfilename) = File::Spec->splitpath( $0 );		# Get volume, directory and filename info from current script name $0

my $currentscriptfilenameonly = substr( $currentscriptfilename, 0, rindex( $currentscriptfilename, '.' ) );
my $currentscriptfilenameextn = substr( $currentscriptfilename, rindex( $currentscriptfilename, '.' ), length( $currentscriptfilename ) );

Log::Log4perl->init( "log4j.properties" );

our $log = Log::Log4perl->get_logger( "My::$currentscriptfilenameonly" );

if ( 0 )					# derek.bolli 20130130 10:23:00 Security feature OFF	
{
	my $username = getlogin || getpwuid($<);

	if ( $username ne "derek.bolli" )
	{
		$log->info( "Unauthorised user. Exiting..." );		# derek.bolli 20130129 10:52:45 Security feature...
		exit;
	}
	
}

if ( @ARGV==0 )
{
  # The number of commandline parameters is 0,
  # so print a Usage message.
  #
  $log->info( "Usage: /usr/bin/perl " . $currentscriptfilenameonly . ".pl filename.ext\n" );
  exit();   # When usage() has completed execution,
            # exit the program.
}

$log->info( "Running script $currentscriptfilenameonly with extension $currentscriptfilenameextn..." );		# DEBUG
		
srand();				# Init random number seed with default time

#my $ini = new Config::IniFiles( -file => "./$currentscriptfilenameonly.ini" );			# open .ini file with same name as script

use File::Slurp;

my $inputfile = read_file( $ARGV[0] ) or die "Can't open file to convert (" . $ARGV[0] . "): $!";

use Data::HexDump;
$log->info( "Hex Dump of " . $ARGV[0] . ":\n" . HexDump( $inputfile ) );

(my $inputfilevolume, my $inputfiledirectories, our $inputfilefilename) = File::Spec->splitpath( $ARGV[0] );		# Get volume, directory and filename info from input file $ARGV[0]

#$log->info( "Volume: " . $inputfilevolume );			# :dbolli:20140321 12:09:06 DEBUG
$log->info( "Directories: " . $inputfiledirectories );			# :dbolli:20140321 12:09:06 DEBUG
$log->info( "Filename: " . $inputfilefilename );			# :dbolli:20140321 12:09:06 DEBUG

my $inputfilefilenameonly = substr( $ARGV[0], 0, rindex( $ARGV[0], '.' ) );

open( OUTFILE,  ">" . $inputfiledirectories . "hex-dump-" . $inputfilefilename . "-" . getDateTimeStamp() . ".txt" )   or die "Can't open: $!";

print OUTFILE HexDump( $inputfile ), "\n";

close OUTFILE;

#exit;		# will never get here as die will be executed for blank line at top of while loop above

END { 
      
	$log->info( "Cleaning up..." ); 
	# clean up code here

	# my $username = getlogin || getpwuid($<);

	# my $tranfh = IO::File->new( ">./" . our $testresultsfolder . "/test results " . our $currentscriptfilename . " " . $username . "-" . getDateTimeStamp() . ".txt" );
	# $tranfh->close;

};		# as per http://www.linuxquestions.org/questions/programming-9/in-perl-is-there-a-way-when-i-call-exit-function-to-execute-a-subroutine-558220/