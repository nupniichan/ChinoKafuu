FROM openjdk:17-oracle

WORKDIR /app

COPY Lavalink.jar /app/
COPY application.yml /app/
COPY youtube-plugin-1.8.3.jar /app/

EXPOSE 2333

CMD ["java", "-jar", "Lavalink.jar"]