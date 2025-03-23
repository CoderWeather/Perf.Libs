dotnet pack \
  ../PerfHolders \
  -p:NuspecFile=../nuget/PerfHolders.nuspec \
  -c Release \
  -o ../../../nuget/
dotnet pack ../PerfHolders.Serialization.SystemTextJson \
  -c Release \
  -o ../../../nuget/
dotnet pack ../PerfHolders.Serialization.MessagePack \
  -c Release \
  -o ../../../nuget/
