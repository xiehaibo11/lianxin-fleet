# You can develop your own plugins to make API calls. Please refer to this illustrated guide on how to use the extended API: https://prodocs.lceda.cn/cn/api/guide/how-to-start.html

# Please take a look at the terminal CLI reference here:
https://prodocs.lceda.cn/cn/api/guide/cli.html

## The claim regarding wiring rules remains to be verified.

1. Minimize Interconnections: Reduce the number of traces as much as possible during layout, and minimize the number of turns.
2. Avoid Right-Angle Traces: Use 45° angles or arcs instead of 90° right-angle turns.
3. Keep Differential Pairs Together: The two traces of a differential signal must remain parallel and equal in length; do not route them separately.
4. Use Serpentine Traces for Length Matching: Employ serpentine routing to ensure that signal path lengths are identical.
5. Smooth Routing: When changing the direction of a trace, use arcs for transitions to avoid sharp corners.
6. Separate Digital and Analog Circuits: Route digital and analog circuits separately to prevent high-frequency noise crosstalk, which can lead to signal distortion, reference drift, and reduced sampling accuracy.
7. 3W Rule: For two adjacent parallel signal traces, the minimum distance between their centerlines should be at least three times the width of one of the traces.
8. 20H Rule: The edge of the power plane should be recessed inward relative to the ground plane by a distance at least 20 times the thickness of the dielectric layer.