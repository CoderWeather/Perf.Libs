// namespace GeneratorTester;

using Perf.Holders.Result;

readonly partial struct Basic1 : IResultHolder<long, string>;
readonly partial struct Basic2 : Perf.Holders.Result.IResultHolder<long, string>;

readonly partial struct Maybe1 : IOptionHolder<long>;
readonly partial struct Maybe2 : Perf.Holders.Result.IOptionHolder<long>;
