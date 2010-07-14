version = File.read(File.expand_path("../ROUNDHOUSE_VERSION",__FILE__)).strip

Gem::Specification.new do |s|
  s.platform    = Gem::Platform::RUBY
  s.name        = 'roundhouse'
  s.version     = version
  s.files = Dir['lib/**/*'] + Dir['bin/**/*']
  s.bindir = 'bin'
  s.executables << 'rh.exe'
  
  s.summary     = 'RoundhousE - Professional Database Change and Versioning Management'
  s.description = 'RoundhousE - Professional Database Change and Versioning Management'
  
  s.author            = 'Rob "FerventCoder" Reynolds'
  s.email             = 'chucknorrisframework@googlegroups.com'
  s.homepage          = 'http://projectroundhouse.org'
  s.rubyforge_project = 'roundhouse'
end