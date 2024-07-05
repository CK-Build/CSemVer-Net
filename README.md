# CSemVer
![Nuget](https://img.shields.io/nuget/v/CSemVer?logo=nuget)

This package (netstandard2.1, no dependencies) implements https://csemver.org/
that defines a subset of https://semver.org/ (v2.0.0).
It offers parsing an model of regular semver versions and of CSemVer versions, a model of a
unified "version range" and handles Npm and NuGet syntax.

## About versioning
Versions are useless without Version Ranges. Nothing is simple in this domain: see 
https://iscinumpy.dev/post/bound-version-constraints/ for a very good overview of
the numerous aspects and complexities of managing versions.

## Questions and answers
### Why working on a subset?
To be able to reason about versions. The semver specification is great but working 
with semver versions is not that easy, notably because of the lack of "post release"
versions (the automatic version names generated by a continuous integration process).

Thanks to the formal CSemver subset definition, "post releases" are correctly handled,
the set of versions (including automated CI version) is formally defined, and
one can reason about versions: any version has a set of possible next (and previous)
versions.

### Is there anything else?
Yes. A lot. This package introduces a mathematically sound extension to CSemVer by
supporting "package quality" and "version locking" mechanisms.

The goal is, once we can reason about versions, is to handle "version ranges": how
versioning constraints should be expressed to ultimately resolve to the "best" version
of a component among a set of candidates.

The [`SVersionBound`](CSemVer/SVersionBound.cs) describes such a constraint that is
an element of a [lattice](https://en.wikipedia.org/wiki/Lattice_(order).

### Is this an "opinionated library"?
Maybe. At least it is "mathematically sound" and up to me, logic is everything
but opinions...

And this is more than just a "library": what matters is its "content", the fact that it
is implemented in C# and handles NuGet end NPM versioning (the best it can) is an
implementation detail.

### Is it "done"?
Almost, given the current state of the semver specification.

Projections, interpretations for NuGet and NPM are what they are and can certainly be
discussed and enhanced.

One important thing that is currently missing is a `SVersionBoundFormula` that would
be a logical proposition of more than one `SVersionBound` connected by `or`, `and`
(and even `not`) operators to express complex and composite bounds.

Currently, `SVersionBound` can be unioned and intersected to produce another
`SVersionBound` (their [infimum and supremum](https://en.wikipedia.org/wiki/Infimum_and_supremum)).

## NuGet support
TODO

## NPM support
TODO

