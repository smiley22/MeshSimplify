Synopsis
========

MeshSimplify [*options*] [*input-file*]…

Description
===========

MeshSimplify is a program for polygon mesh simplification. It can be used to create simplified variants of an input mesh with arbitrary numbers of faces. In addition, the program is able to produce progressive-mesh representations as part of the simplification process which can be used for smooth multi-resolutional rendering of 3d objects.

Using `MeshSimplify`
--------------------

At its most basic level `MeshSimplify` expects the desired target number of faces as well as an input mesh to create the simplification from. Input files must generally be the .obj wavefront file-format. For specifying the target number of faces, use the `-n` option:

    MeshSimplify -n 1000 bunny.obj

By default, `MeshSimplify` produces an output file with the same name as the input file suffixed by \_out, so the above example would produce the simplified variant of the input file as bunny\_out.obj. To specify a different output location, you can use the `-o` option:

    MeshSimplify -n 1000 -o output.obj bunny.obj

In order to have `MeshSimplify` add vertex-split records to the resulting output file and produce a progressive-mesh, you can specify the `-p` option:

    MeshSimplify -n 1000 -p -o pm-bunny.obj bunny.obj
	
Finally, `MeshSimplify` is also able to restore the original mesh from a progressive-mesh representation or to expand the progressive-mesh to an arbitrary number of faces by specifying the `--restore-mesh` option. So, to expand the previously created progressive-mesh to a polycount of 20000, you could use:

    MeshSimplify -n 20000 --restore-mesh pm-bunny.obj

For a detailed description of all available options, please refer to the next section.

Options
=======

`-a` *ALGORITHM*, `--algorithm` *ALGORITHM*  
Specifies the simplification algorithm to use. Currently the only valid value for this option is `PairContract` which is an implementation of M. Garland’s ‘’Surface Simplification Using Quadric Error Metrics’’ approach. Additional algorithms may be supported in the future.

`-d` *DISTANCE*, `--distance-threshold` *DISTANCE*  
Specifies the distance-threshold value for pair-contraction. This option is only applicable for the `PairContract` algorithm and defaults to 0.

`-h`, `--help`  
Shows usage message.

`-n`, `--num-faces`  
Specifies the number of faces to reduce the input mesh to.

`-o` *FILE*  
Specifies the output file.

`-p`, `--progressive-mesh`  
Adds vertex-split records to the resulting .obj file, effectively creating a progressive-mesh representation of the input mesh.

`-r`, `--restore-mesh`  
Expands the input progressive-mesh to the desired face-count. If this option is specified, the `-n` option refers to the number of faces to restore the output mesh to.

`-s`, `--strict`  
Specifies strict mode. This will cause simplification to fail if the input mesh is malformed or any other anomaly occurs.

`-v`, `--verbose`  
Produces verbose output.

`--version`  
Prints version information.

Authors
=======

© 2015 Torben Könke (torben dot koenke at gmail dot com).

License
=======

This program is released under the MIT license.
