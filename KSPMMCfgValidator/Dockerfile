FROM mono:latest
RUN /bin/sed -i 's/^mozilla\/DST_Root_CA_X3.crt$/!mozilla\/DST_Root_CA_X3.crt/' /etc/ca-certificates.conf \
    && /usr/sbin/update-ca-certificates
ADD KSPMMCfgValidator.exe /usr/local/bin/.
ENTRYPOINT ["mono", "/usr/local/bin/KSPMMCfgValidator.exe"]
