catch {console show}
console title "Battleship Player"
wm withdraw .

puts "Starting..."

# return random number from 0 to n inclusive
proc random n {
    return [expr {int(rand() * ($n + 1))}]
}

set portToUse 0

if { $argc >= 1 } {
	set portToUse [lindex $argv 0]
} else {
	set portToUse 9900
}

puts $portToUse

set server localhost
set sockChan [socket $server $portToUse]

fileevent $sockChan readable [list gets $sockChan ::line]

while {1} {
    vwait ::line
    puts $line
    
    if {[lindex $::line 0] == "SHOOT"} {
        set boatpos [random 99]             ;# random spot 0-99
        while {[lindex $::line [expr {$boatpos + 1}]] != 0} {
            set boatpos [random 99]
        }
        puts $sockChan $boatpos
    }
    flush $sockChan
}
